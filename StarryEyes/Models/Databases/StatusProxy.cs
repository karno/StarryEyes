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
        public static async Task StoreStatusAsync([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (status.RetweetedOriginal != null)
            {
                await StoreStatusAsync(status.RetweetedOriginal);
            }
            var mappedStatus = Mapper.Map(status);
            var mappedUser = Mapper.Map(status.User);
            try
            {
                if (!(await Database.StatusCrud.CheckExistsAsync(status.Id)))
                {
                    await Database.StoreStatus(
                        mappedStatus.Item1, mappedStatus.Item2,
                        mappedUser.Item1, mappedUser.Item2, mappedUser.Item3);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task<IEnumerable<long>> GetRetweetedStatusIds(long originalId)
        {
            try
            {
                var rts = await Database.StatusCrud.GetRetweetedStatusesAsync(originalId);
                return rts.Select(r => r.Id);
            }
            catch
            {
                return Enumerable.Empty<long>();
            }
        }

        public static async Task RemoveStatusAsync(long statusId)
        {
            try
            {
                // remove retweets
                await Database.StatusCrud.DeleteAsync(statusId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

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
                await Database.FavoritesCrud.RemoveWhereAsync(statusId, userId);
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
                await Database.RetweetsCrud.RemoveWhereAsync(statusId, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public static async Task<bool> IsStatusExistsAsync(long id)
        {
            return await Database.StatusCrud.GetAsync(id) != null;
        }

        public static async Task<TwitterStatus> GetStatusAsync(long id)
        {
            var status = await Database.StatusCrud.GetAsync(id);
            if (status == null) return null;
            return await LoadStatusAsync(status);
        }

        public static async Task<long?> GetInReplyToAsync(long id)
        {
            return await Database.StatusCrud.GetInReplyToAsync(id);
        }

        public static async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return await Database.StatusCrud.FindFromInReplyToAsync(inReplyTo);
        }

        public static IObservable<TwitterStatus> FetchStatuses(string sql, long? maxId = null, int? count = null, bool applyMuteBlockFilter = true)
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
                sql += " limit " + count.Value.ToString(CultureInfo.InvariantCulture);
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
            if (dbstatus.RetweetOriginalId == null)
            {
                return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await user);
            }
            var rts = GetStatusAsync(dbstatus.RetweetOriginalId.Value);
            return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await rts, await user);
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
