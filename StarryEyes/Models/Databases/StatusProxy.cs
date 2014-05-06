using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Databases.Caching;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Databases
{
    public static class StatusProxy
    {
        private static readonly TaskQueue<long, TwitterStatus> _statusQueue;

        static StatusProxy()
        {
            _statusQueue = new TaskQueue<long, TwitterStatus>(200, TimeSpan.FromSeconds(30),
                async s => await StoreStatusesAsync(s));
            _favoriteQueue = new ActivityQueue(50, TimeSpan.FromSeconds(30),
                s => Task.Run(() => Database.FavoritesCrud.InsertAllAsync(s)),
                s => Task.Run(() => Database.FavoritesCrud.DeleteAllAsync(s)));
            _retweetQueue = new ActivityQueue(50, TimeSpan.FromSeconds(30),
                s => Task.Run(() => Database.RetweetsCrud.InsertAllAsync(s)),
                s => Task.Run(() => Database.RetweetsCrud.DeleteAllAsync(s)));
            App.ApplicationFinalize += () =>
            {
                _statusQueue.Writeback();
                _favoriteQueue.Writeback();
                _retweetQueue.Writeback();
            };
        }

        public static Task<long> GetCountAsync()
        {
            return Database.StatusCrud.GetCountAsync();
        }

        #region Store and remove statuses

        /// <summary>
        /// Enqueue store status 
        /// </summary>
        /// <param name="status"></param>
        public static void StoreStatus([NotNull] TwitterStatus status)
        {
            _statusQueue.Enqueue(status.Id, status);
            StatisticsService.SetQueuedStatusCount(_statusQueue.Count);
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
            await DatabaseUtil.RetryIfLocked(async () => await Database.StoreStatuses(store));
            StatisticsService.SetQueuedStatusCount(_statusQueue.Count);
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
                await DatabaseUtil.RetryIfLocked(
                    async () => await Database.StatusCrud.DeleteAllAsync(removals));
                return removals;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Enumerable.Empty<long>();
            }
        }

        public static async Task<bool> IsStatusExistsAsync(long id)
        {
            if (_statusQueue.Contains(id)) return true;
            return await DatabaseUtil.RetryIfLocked(
                async () => await Database.StatusCrud.GetAsync(id)) != null;
        }

        public static async Task<TwitterStatus> GetStatusAsync(long id)
        {
            TwitterStatus cache;
            if (_statusQueue.TryGetValue(id, out cache))
            {
                return cache;
            }
            var status = await DatabaseUtil.RetryIfLocked(
                async () => await Database.StatusCrud.GetAsync(id));
            if (status == null) return null;
            return await LoadStatusAsync(status);
        }

        public static async Task<long?> GetInReplyToAsync(long id)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
            {
                TwitterStatus cache;
                if (_statusQueue.TryGetValue(id, out cache))
                {
                    return cache.InReplyToStatusId;
                }
                return await Database.StatusCrud.GetInReplyToAsync(id);
            });
        }

        public static async Task<IEnumerable<long>> GetRetweetedStatusIds(long originalId)
        {
            try
            {
                var rts = await DatabaseUtil.RetryIfLocked(
                    async () => await Database.StatusCrud.GetRetweetedStatusesAsync(originalId));
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

        public static async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            var replies = await DatabaseUtil.RetryIfLocked(
                async () => await Database.StatusCrud.FindFromInReplyToAsync(inReplyTo));
            return replies.Concat(_statusQueue.Find(s => s.InReplyToStatusId == inReplyTo)
                                              .Select(s => s.Id));
        }

        #endregion

        #region Favorites and retweets

        private static readonly ActivityQueue _favoriteQueue;

        private static readonly ActivityQueue _retweetQueue;

        public static void AddFavoritor(long statusId, long userId)
        {
            _favoriteQueue.Add(statusId, userId);
        }

        public static void RemoveFavoritor(long statusId, long userId)
        {
            _favoriteQueue.Remove(statusId, userId);
        }

        public static void AddRetweeter(long statusId, long userId)
        {
            _retweetQueue.Add(statusId, userId);
        }

        public static void RemoveRetweeter(long statusId, long userId)
        {
            _retweetQueue.Remove(statusId, userId);
        }

        public static async Task<TwitterStatus> SyncStatusActivityAsync(TwitterStatus status)
        {
            if (status.StatusType == StatusType.Tweet)
            {
                IEnumerable<long> favadd, favremove, rtadd, rtremove;
                _favoriteQueue.GetDirtyActivity(status.Id, out favadd, out favremove);
                _retweetQueue.GetDirtyActivity(status.Id, out rtadd, out rtremove);
                var favorers = await DatabaseUtil.RetryIfLocked(
                    async () => await Database.FavoritesCrud.GetUsersAsync(status.Id));
                var retweeters = await DatabaseUtil.RetryIfLocked(
                    async () => await Database.RetweetsCrud.GetUsersAsync(status.Id));
                status.FavoritedUsers = favorers.Guard().Concat(favadd).Except(favremove).ToArray();
                status.RetweetedUsers = retweeters.Guard().Concat(rtadd).Except(rtremove).ToArray();
            }
            return status;
        }

        #endregion

        public static IObservable<TwitterStatus> FetchStatuses(
            Func<TwitterStatus, bool> predicate, string sql,
            long? maxId = null, int? count = null, bool applyMuteBlockFilter = true)
        {
            var cache = FindCache(predicate, maxId, applyMuteBlockFilter);
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

            // find status and limit caches
            var read = Task.Run(async () =>
            {
                var fetched = (await Database.StatusCrud.FetchAsync(sql)).ToArray();

                var bcache = cache;
                if (count != null && fetched.Length == count)
                {
                    // if database can yield more results, limit cache results.

                    // find last id of database result
                    var lid = fetched[fetched.Length - 1].Id;

                    // limiting cache result
                    cache = Task.Run(async () =>
                    {
                        await Task.Yield();
                        var ext = await bcache;
                        return maxId != null
                            ? ext.Where(s => s.Id > lid && s.Id < maxId)
                            : ext.Where(s => s.Id > lid);
                    });
                }
                return fetched;
            });
            // ReSharper disable once PossibleMultipleEnumeration
            return read.ToObservable()
                       .SelectMany(_ => _)
                       .SelectMany(s => LoadStatusAsync(s).ToObservable())
                       .Merge(cache.ToObservable().SelectMany(_ => _))
                       .Distinct(s => s.Id);
        }

        private static async Task<IEnumerable<TwitterStatus>> FindCache(
            Func<TwitterStatus, bool> predicate, long? maxId = null, bool applyMuteBlockFilter = true)
        {
            if (maxId != null)
            {
                var op = predicate;
                predicate = s => op(s) && s.Id < maxId;
            }
            if (applyMuteBlockFilter)
            {
                var op = predicate;
                predicate = s => op(s) && !MuteBlockManager.IsUnwanted(s);
            }
            return await Task.Run(() => _statusQueue.Find(predicate));
        }

        #region Load from database

        private static async Task<TwitterStatus> LoadStatusAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            return await DatabaseUtil.RetryIfLocked(async () =>
            {
                switch (dbstatus.StatusType)
                {
                    case StatusType.Tweet:
                        return await LoadPublicStatusAsync(dbstatus);
                    case StatusType.DirectMessage:
                        return await LoadDirectMessageAsync(dbstatus);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private static async Task<TwitterStatus> LoadPublicStatusAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            var id = dbstatus.Id;
            var user = DatabaseUtil.RetryIfLocked(async () =>
                await UserProxy.GetUserAsync(dbstatus.UserId));
            var se = DatabaseUtil.RetryIfLocked(async () =>
                await Database.StatusEntityCrud.GetEntitiesAsync(id));
            var favorers = DatabaseUtil.RetryIfLocked(async () =>
                await Database.FavoritesCrud.GetUsersAsync(id));
            var retweeters = DatabaseUtil.RetryIfLocked(async () =>
                await Database.RetweetsCrud.GetUsersAsync(id));
            try
            {
                if (dbstatus.RetweetOriginalId != null)
                {
                    var orig = await GetStatusAsync(dbstatus.RetweetOriginalId.Value);
                    if (orig != null)
                    {
                        return Mapper.Map(dbstatus,
                            await se,
                            await favorers,
                            await retweeters,
                            orig, await user);
                    }
                }
                return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await user);
            }
            catch (ArgumentNullException anex)
            {
                throw new DatabaseConsistencyException(
                    "データベースから必要なデータを読み出せませんでした。(モード: PS, ステータスID " + dbstatus.Id + ", ユーザID " + dbstatus.UserId + ")",
                    anex);
            }
        }

        private static async Task<TwitterStatus> LoadDirectMessageAsync([NotNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            if (dbstatus.InReplyToOrRecipientUserId == null)
                throw new ArgumentException("dbstatus.InReplyToUserOrRecipientId is must not be null.");
            var id = dbstatus.Id;
            var user = UserProxy.GetUserAsync(dbstatus.UserId);
            var recipient = UserProxy.GetUserAsync(dbstatus.InReplyToOrRecipientUserId.Value);
            var se = Database.StatusEntityCrud.GetEntitiesAsync(id);
            try
            {
                return Mapper.Map(dbstatus,
                    await DatabaseUtil.RetryIfLocked(async () => await se),
                    await user, await recipient);
            }
            catch (ArgumentNullException anex)
            {
                throw new DatabaseConsistencyException(
                    "データベースから必要なデータを読み出せませんでした。(モード: DM, ステータスID " + dbstatus.Id + ", ユーザID " + dbstatus.UserId + ")",
                    anex);
            }
        }

        #endregion
    }
}
