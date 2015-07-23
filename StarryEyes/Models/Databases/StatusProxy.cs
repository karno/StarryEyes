using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                if (s.RetweetedStatus != null)
                {
                    return new[] { s.RetweetedStatus, s };
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
                          .Concat(_statusQueue.Find(s => s.RetweetedStatusId == originalId)
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
                var favorers = DatabaseUtil.RetryIfLocked(
                    async () => await Database.FavoritesCrud.GetUsersAsync(status.Id));
                var retweeters = DatabaseUtil.RetryIfLocked(
                    async () => await Database.RetweetsCrud.GetUsersAsync(status.Id));
                status.FavoritedUsers = (await favorers).Guard().Concat(favadd).Except(favremove).ToArray();
                status.RetweetedUsers = (await retweeters).Guard().Concat(rtadd).Except(rtremove).ToArray();
            }
            return status;
        }

        #endregion

        public static Task<IEnumerable<TwitterStatus>> FetchStatuses(
            Func<TwitterStatus, bool> predicate, string sql,
            long? maxId = null, int? count = null, bool applyMuteBlockFilter = true)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            var cacheReader = FindCache(predicate, maxId, applyMuteBlockFilter);
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
                var db = await Database.StatusCrud.FetchAsync(sql);
                var fetched = (await LoadStatusesAsync(db))
                    .OrderByDescending(d => d.Id)
                    .ToArray();

                // await finding cache
                var cachedStatus = (await cacheReader).OrderByDescending(d => d.Id).AsEnumerable();

                if (count != null && fetched.Length == count)
                {
                    // if database can yield more results, trim cached results
                    // by maximum ID of database status.

                    var lid = fetched[fetched.Length - 1].Id;

                    cachedStatus = cachedStatus.TakeWhile(d => d.Id < lid);
                }
                return cachedStatus.Concat(fetched)
                                   .Distinct(d => d.Id)
                                   .OrderByDescending(d => d.CreatedAt)
                                   .AsEnumerable();
            });
            // ReSharper disable once PossibleMultipleEnumeration
            return read;
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
                    "Lacking required data in database.(mode: PS, status ID " + dbstatus.Id + ", user ID " + dbstatus.UserId + ")",
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
                    "Lacking required data in database.(mode: DM, status ID " + dbstatus.Id + ", user ID " + dbstatus.UserId + ")",
                    anex);
            }
        }

        private static async Task<IEnumerable<TwitterStatus>> LoadStatusesAsync([NotNull] IEnumerable<DatabaseStatus> dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException("dbstatus");
            var targets = dbstatus.ToArray();
            if (targets.Length == 0)
            {
                return Enumerable.Empty<TwitterStatus>();
            }
            var retweetOriginalIds = new HashSet<long>();
            var targetUserIds = new HashSet<long>();
            var entitiesTargetIds = new HashSet<long>();
            var activitiesTargetIds = new HashSet<long>();
            foreach (var status in targets)
            {
                targetUserIds.Add(status.UserId);
                entitiesTargetIds.Add(status.Id);
                switch (status.StatusType)
                {
                    case StatusType.Tweet:
                        activitiesTargetIds.Add(status.Id);
                        if (status.RetweetOriginalId != null)
                        {
                            Debug.Assert(status.RetweetOriginalUserId != null,
                                "status.RetweetOriginalUserId != null");
                            targetUserIds.Add(status.RetweetOriginalUserId.Value);
                            retweetOriginalIds.Add(status.RetweetOriginalId.Value);
                            entitiesTargetIds.Add(status.RetweetOriginalId.Value);
                        }
                        break;
                    case StatusType.DirectMessage:
                        Debug.Assert(status.InReplyToOrRecipientUserId != null,
                            "status.InReplyToOrRecipientUserId != null");
                        targetUserIds.Add(status.InReplyToOrRecipientUserId.Value);
                        break;
                }
            }
            // accessing database
            var retweetsTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.StatusCrud.GetStatusesAsync(retweetOriginalIds));
            var usersTask = UserProxy.GetUsersAsync(targetUserIds);
            var sesTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.StatusEntityCrud.GetEntitiesDictionaryAsync(entitiesTargetIds));
            var favdicTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.FavoritesCrud.GetUsersDictionaryAsync(activitiesTargetIds));
            var rtdicTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.RetweetsCrud.GetUsersDictionaryAsync(activitiesTargetIds));
            var retweets = (await retweetsTask).ToDictionary(d => d.Id);
            var users = (await usersTask).ToDictionary(d => d.Id);
            var ses = await sesTask;
            var favdic = await favdicTask;
            var rtdic = await rtdicTask;

            // create status entity
            var result = new List<TwitterStatus>();
            foreach (var status in targets)
            {
                var ents = Mapper.Resolve(ses, status.Id);
                switch (status.StatusType)
                {
                    case StatusType.Tweet:
                        var favs = Mapper.Resolve(favdic, status.Id);
                        var rts = Mapper.Resolve(rtdic, status.Id);
                        if (status.RetweetOriginalId != null)
                        {
                            Debug.Assert(status.RetweetOriginalUserId != null,
                                "status.RetweetOriginalUserId != null");
                            var rtid = status.RetweetOriginalId.Value;
                            var retweet = retweets[status.RetweetOriginalId.Value];
                            var orig = Mapper.Map(retweet, Mapper.Resolve(ses, rtid),
                                Mapper.Resolve(favdic, rtid), Mapper.Resolve(rtdic, rtid),
                                users[status.RetweetOriginalUserId.Value]);
                            result.Add(Mapper.Map(status, ents, favs, rts, orig, users[status.UserId]));
                        }
                        else
                        {
                            result.Add(Mapper.Map(status, ents, favs, rts, users[status.UserId]));
                        }
                        break;
                    case StatusType.DirectMessage:
                        Debug.Assert(status.InReplyToOrRecipientUserId != null,
                                            "status.InReplyToOrRecipientUserId != null");
                        result.Add(Mapper.Map(status, ents, users[status.UserId],
                            users[status.InReplyToOrRecipientUserId.Value]));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return result;
        }


        #endregion
    }
}
