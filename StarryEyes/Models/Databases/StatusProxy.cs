using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Receiving;

namespace StarryEyes.Models.Databases
{
    public static class StatusProxy
    {
        private static readonly TaskQueue<long, TwitterStatus> _statusQueue;

        static StatusProxy()
        {
            _statusQueue = new TaskQueue<long, TwitterStatus>(200, async s => await StoreStatusesAsync(s));
        }

        public static Task<long> GetCountAsync()
        {
            return Database.StatusCrud.GetCountAsync();
        }

        #region Storing and removing status

        /// <summary>
        /// Queue store status 
        /// </summary>
        /// <param name="status"></param>
        public static void StoreStatus([NotNull] TwitterStatus status)
        {
            _statusQueue.Enqueue(status.Id, status);
        }

        private static async Task StoreStatusesAsync(IEnumerable<TwitterStatus> statuses)
        {
            // extracting retweeted status
            var store = statuses.SelectMany(s =>
            {
                if (s.RetweetedOriginal != null)
                {
                    return new[] { s.RetweetedOriginal, s };
                }
                return new[] { s };
            }).Select(s => StatusInsertBatch.CreateBatch(Mapper.Map(s), Mapper.Map(s.User)));
            await Database.StoreStatuses(store);
        }

        /// <summary>
        /// Remove statuses
        /// </summary>
        /// <param name="statusId">target status</param>
        /// <returns>removed status ids</returns>
        public static async Task<IEnumerable<long>> RemoveStatusAsync(long statusId)
        {
            try
            {
                // remove queued statuses
                _statusQueue.Remove(statusId);

                // find retweets, remove from queue and concatenate id
                var removals = (await GetRetweetedStatusIds(statusId))
                    .Do(id => _statusQueue.Remove(id)) // remove from queue
                    .Concat(new[] { statusId }) // concat original id
                    .ToArray();

                // remove status
                await Database.StatusCrud.DeleteAllAsync(removals);
                return removals;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Enumerable.Empty<long>();
            }
        }

        #endregion

        public static async Task<IEnumerable<long>> GetRetweetedStatusIds(long originalId)
        {
            try
            {
                var rts = await Database.StatusCrud.GetRetweetedStatusesAsync(originalId);
                return rts.Select(r => r.Id)
                          .Concat(_statusQueue.Find(s => s.RetweetedOriginalId == originalId)
                                              .Select(s => s.Id));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Enumerable.Empty<long>();
            }
        }

        #region Favorites and retweets

        public static async Task AddFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.InsertAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task RemoveFavoritorAsync(long statusId, long userId)
        {
            try
            {
                await Database.FavoritesCrud.DeleteAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task AddRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.InsertAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task RemoveRetweeterAsync(long statusId, long userId)
        {
            try
            {
                await Database.RetweetsCrud.DeleteAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        #endregion

        public static async Task<bool> IsStatusExistsAsync(long id)
        {
            if (_statusQueue.Contains(id)) return true;
            return await Database.StatusCrud.GetAsync(id) != null;
        }

        public static async Task<TwitterStatus> GetStatusAsync(long id)
        {
            var status = await Database.StatusCrud.GetAsync(id);
            if (status == null) return null;
            return await LoadStatusAsync(status);
        }

        public static async Task<TwitterStatus> SyncStatusActivityAsync(TwitterStatus status)
        {
            if (status.StatusType == StatusType.Tweet)
            {
                var favorers = await Database.FavoritesCrud.GetUsersAsync(status.Id);
                var retweeters = await Database.RetweetsCrud.GetUsersAsync(status.Id);
                status.FavoritedUsers = favorers.Guard().ToArray();
                status.RetweetedUsers = retweeters.Guard().ToArray();
            }
            return status;
        }

        public static async Task<long?> GetInReplyToAsync(long id)
        {
            return await Database.StatusCrud.GetInReplyToAsync(id);
        }

        public static async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return await Database.StatusCrud.FindFromInReplyToAsync(inReplyTo);
        }

        public static IObservable<TwitterStatus> FetchStatuses(
            string sql, long? maxId = null, int? count = null, bool applyMuteBlockFilter = true)
        {
            if (maxId != null)
            {
                var midc = "Id < " + maxId.Value.ToString(CultureInfo.InvariantCulture);
                sql = sql.SqlConcatAnd(midc);
            }
            if (applyMuteBlockFilter)
            {
                sql = sql.SqlConcatAnd(MuteBlockManager.FilteringSql);
            }
            if (!String.IsNullOrEmpty(sql))
            {
                sql = " where " + sql;
            }
            if (count != null)
            {
                sql += " order by Id desc limit " + count.Value.ToString(CultureInfo.InvariantCulture);
            }
            sql = "select * from status" + sql + ";";
            return Database.StatusCrud.FetchAsync(sql)
                           .ToObservable()
                           .SelectMany(_ => _)
                           .SelectMany(s => LoadStatusAsync(s).ToObservable());
        }

        public static Task<TwitterStatus> LoadStatusAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            switch (dbstatus.StatusType)
            {
                case StatusType.Tweet:
                    return LoadPublicStatusAsync(dbstatus);
                case StatusType.DirectMessage:
                    return LoadDirectMessageAsync(dbstatus);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static async Task<TwitterStatus> LoadPublicStatusAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            var id = dbstatus.Id;
            var user = UserProxy.GetUserAsync(dbstatus.UserId);
            var se = Database.StatusEntityCrud.GetEntitiesAsync(id);
            var favorers = Database.FavoritesCrud.GetUsersAsync(id);
            var retweeters = Database.RetweetsCrud.GetUsersAsync(id);
            try
            {
                if (dbstatus.RetweetOriginalId != null)
                {
                    var orig = await GetStatusAsync(dbstatus.RetweetOriginalId.Value);
                    if (orig != null)
                    {
                        return Mapper.Map(dbstatus, await se, await favorers, await retweeters, orig, await user);
                    }
                }
                return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await user);
            }
            catch (ArgumentNullException anex)
            {
                throw new DatabaseConsistencyException(
                    "データベースから必要なデータを読み出せませんでした。(ステータスID " + dbstatus.Id + ", ユーザID " + dbstatus.UserId + ")",
                    anex);
            }
        }

        private static async Task<TwitterStatus> LoadDirectMessageAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            if (dbstatus.InReplyToOrRecipientUserId == null) throw new ArgumentException("dbstatus.InReplyToUserOrRecipientId is must not be null.");
            var id = dbstatus.Id;
            var user = UserProxy.GetUserAsync(dbstatus.UserId);
            var recipient = UserProxy.GetUserAsync(dbstatus.InReplyToOrRecipientUserId.Value);
            var se = Database.StatusEntityCrud.GetEntitiesAsync(id);
            return Mapper.Map(dbstatus, await se, await user, await recipient);
        }

    }
}
