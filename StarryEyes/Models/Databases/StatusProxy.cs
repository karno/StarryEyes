using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Databases.Caching;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Databases
{
    public static class StatusProxy
    {
        private static readonly DatabaseWriterQueue<long, TwitterStatus> _statusQueue;

        static StatusProxy()
        {
            _statusQueue = new DatabaseWriterQueue<long, TwitterStatus>(200, TimeSpan.FromSeconds(30),
                async s => await StoreStatusesAsync(s).ConfigureAwait(false));
            _favoriteQueue = new ActivityQueue(50, TimeSpan.FromSeconds(30),
                s => Task.Run(() => Database.FavoritesCrud.InsertAllAsync(s)),
                s => Task.Run(() => Database.FavoritesCrud.DeleteAllAsync(s)));
            _retweetQueue = new ActivityQueue(50, TimeSpan.FromSeconds(30),
                s => Task.Run(() => Database.RetweetsCrud.InsertAllAsync(s)),
                s => Task.Run(() => Database.RetweetsCrud.DeleteAllAsync(s)));
            App.ApplicationFinalize += () =>
            {
                _statusQueue.Dispose();
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
            var store = statuses.SelectMany(s => new[] { s, s.RetweetedStatus, s.QuotedStatus })
                                .Where(s => s != null)
                                .Select(s => StatusInsertBatch.CreateBatch(Mapper.Map(s), Mapper.Map(s.User),
                                    s.Recipient != null ? Mapper.Map(s.Recipient) : null));
            await DatabaseUtil.RetryIfLocked(() => Database.StoreStatuses(store)).ConfigureAwait(false);
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
                var removals = (await GetRetweetedStatusIds(statusId).ConfigureAwait(false))
                    .Do(id => _statusQueue.Remove(id)) // remove from queue
                    .Concat(new[] { statusId }) // concat original id
                    .ToArray();

                // remove status
                await DatabaseUtil.RetryIfLocked(() => Database.StatusCrud.DeleteAllAsync(removals))
                                  .ConfigureAwait(false);
                return removals;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return Enumerable.Empty<long>();
            }
        }

        public static async Task<bool> IsStatusExistsAsync(long id)
        {
            if (_statusQueue.Contains(id)) return true;
            return await DatabaseUtil.RetryIfLocked(
                       () => Database.StatusCrud.GetAsync(id)).ConfigureAwait(false) != null;
        }

        public static async Task<TwitterStatus> GetStatusAsync(long id)
        {
            TwitterStatus cache;
            if (_statusQueue.TryGetValue(id, out cache))
            {
                return cache;
            }
            var status = await DatabaseUtil.RetryIfLocked(() =>
                Database.StatusCrud.GetAsync(id)).ConfigureAwait(false);
            if (status == null) return null;
            return await LoadStatusAsync(status).ConfigureAwait(false);
        }

        public static async Task<long?> GetInReplyToAsync(long id)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
            {
                TwitterStatus cache;
                return _statusQueue.TryGetValue(id, out cache)
                    ? Task.FromResult(cache.InReplyToStatusId)
                    : Database.StatusCrud.GetInReplyToAsync(id);
            }).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<long>> GetRetweetedStatusIds(long originalId)
        {
            try
            {
                var rts = await DatabaseUtil.RetryIfLocked(() =>
                                                Database.StatusCrud.GetRetweetedStatusesAsync(originalId))
                                            .ConfigureAwait(false);
                return rts.Select(r => r.Id)
                          .Concat(_statusQueue.Find(s => s.RetweetedStatus?.Id == originalId)
                                              .Select(s => s.Id));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return Enumerable.Empty<long>();
            }
        }

        public static async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            var replies = await DatabaseUtil.RetryIfLocked(() =>
                                                Database.StatusCrud.FindFromInReplyToAsync(inReplyTo))
                                            .ConfigureAwait(false);
            return replies.Concat(_statusQueue.Find(s => s.InReplyToStatusId == inReplyTo)
                                              .Select(s => s.Id));
        }

        #endregion Store and remove statuses

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
                var favorers = DatabaseUtil.RetryIfLocked(() =>
                    Database.FavoritesCrud.GetUsersAsync(status.Id)).ConfigureAwait(false);
                var retweeters = DatabaseUtil.RetryIfLocked(() =>
                    Database.RetweetsCrud.GetUsersAsync(status.Id)).ConfigureAwait(false);
                status.FavoritedUsers = (await favorers).Guard().Concat(favadd).Except(favremove).ToArray();
                status.RetweetedUsers = (await retweeters).Guard().Concat(rtadd).Except(rtremove).ToArray();
            }
            return status;
        }

        #endregion Favorites and retweets

        public static Task<IEnumerable<TwitterStatus>> FetchStatuses(
            Func<TwitterStatus, bool> predicate, string sql,
            long? maxId = null, int? count = null, bool applyMuteBlockFilter = true)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
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
                var db = await Database.StatusCrud.FetchAsync(sql).ConfigureAwait(false);
                var fetched = (await LoadStatusesAsync(db).ConfigureAwait(false))
                    .OrderByDescending(d => d.Id)
                    .ToArray();

                // await finding cache
                var cachedStatus = (await cacheReader.ConfigureAwait(false))
                    .OrderByDescending(d => d.Id).AsEnumerable();

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
            return await Task.Run(() => _statusQueue.Find(predicate)).ConfigureAwait(false);
        }

        #region Load from database

        private static async Task<TwitterStatus> LoadStatusAsync([CanBeNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException(nameof(dbstatus));
            return await DatabaseUtil.RetryIfLocked(() =>
            {
                switch (dbstatus.StatusType)
                {
                    case StatusType.Tweet:
                        return LoadPublicStatusAsync(dbstatus);
                    case StatusType.DirectMessage:
                        return LoadDirectMessageAsync(dbstatus);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }).ConfigureAwait(false);
        }

        private static async Task<TwitterStatus> LoadPublicStatusAsync([CanBeNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException(nameof(dbstatus));
            var id = dbstatus.Id;
            var user = DatabaseUtil.RetryIfLocked(() =>
                UserProxy.GetUserAsync(dbstatus.UserId)).ConfigureAwait(false);
            var se = DatabaseUtil.RetryIfLocked(() =>
                Database.StatusEntityCrud.GetEntitiesAsync(id)).ConfigureAwait(false);
            var favorers = DatabaseUtil.RetryIfLocked(() =>
                Database.FavoritesCrud.GetUsersAsync(id)).ConfigureAwait(false);
            var retweeters = DatabaseUtil.RetryIfLocked(() =>
                Database.RetweetsCrud.GetUsersAsync(id)).ConfigureAwait(false);
            try
            {
                if (dbstatus.RetweetOriginalId != null || dbstatus.QuoteId != null)
                {
                    var orig = dbstatus.RetweetOriginalId == null
                        ? null
                        : await GetStatusAsync(dbstatus.RetweetOriginalId.Value).ConfigureAwait(false);
                    var quote = dbstatus.QuoteId == null
                        ? null
                        : await GetStatusAsync(dbstatus.QuoteId.Value).ConfigureAwait(false);
                    return Mapper.Map(dbstatus,
                        await se,
                        await favorers,
                        await retweeters,
                        orig, quote, await user);
                }
                return Mapper.Map(dbstatus, await se, await favorers, await retweeters, await user);
            }
            catch (ArgumentNullException anex)
            {
                throw new DatabaseConsistencyException(
                    "Lacking required data in database.(mode: PS, status ID " + dbstatus.Id + ", user ID " +
                    dbstatus.UserId + ")",
                    anex);
            }
        }

        private static async Task<TwitterStatus> LoadDirectMessageAsync([CanBeNull] DatabaseStatus dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException(nameof(dbstatus));
            if (dbstatus.InReplyToOrRecipientUserId == null)
                throw new ArgumentException("dbstatus.InReplyToUserOrRecipientId is must not be null.");
            var id = dbstatus.Id;
            var user = UserProxy.GetUserAsync(dbstatus.UserId).ConfigureAwait(false);
            var recipient = UserProxy.GetUserAsync(dbstatus.InReplyToOrRecipientUserId.Value).ConfigureAwait(false);
            var se = DatabaseUtil.RetryIfLocked(() => Database.StatusEntityCrud.GetEntitiesAsync(id))
                                 .ConfigureAwait(false);
            try
            {
                return Mapper.Map(dbstatus, await se, await user, await recipient);
            }
            catch (ArgumentNullException anex)
            {
                throw new DatabaseConsistencyException(
                    "Lacking required data in database.(mode: DM, status ID " + dbstatus.Id + ", user ID " +
                    dbstatus.UserId + ")",
                    anex);
            }
        }

        private static async Task<IEnumerable<TwitterStatus>> LoadStatusesAsync(
            [CanBeNull] IEnumerable<DatabaseStatus> dbstatus)
        {
            if (dbstatus == null) throw new ArgumentNullException(nameof(dbstatus));
            var targets = dbstatus.ToArray();
            if (targets.Length == 0)
            {
                return Enumerable.Empty<TwitterStatus>();
            }
            var additionalStatusIds = new HashSet<long>();
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
                            additionalStatusIds.Add(status.RetweetOriginalId.Value);
                            entitiesTargetIds.Add(status.RetweetOriginalId.Value);
                        }
                        if (status.QuoteId != null)
                        {
                            Debug.Assert(status.QuoteUserId != null,
                                "status.QuoteUserId != null");
                            targetUserIds.Add(status.QuoteUserId.Value);
                            additionalStatusIds.Add(status.QuoteId.Value);
                            entitiesTargetIds.Add(status.QuoteId.Value);
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
            var additionalStatusesTask = DatabaseUtil.RetryIfLocked(() =>
                                                         Database.StatusCrud.GetStatusesAsync(additionalStatusIds))
                                                     .ConfigureAwait(false);
            var usersTask = UserProxy.GetUsersAsync(targetUserIds).ConfigureAwait(false);
            var sesTask = DatabaseUtil.RetryIfLocked(() =>
                                          Database.StatusEntityCrud.GetEntitiesDictionaryAsync(entitiesTargetIds))
                                      .ConfigureAwait(false);
            var favdicTask = DatabaseUtil.RetryIfLocked(() =>
                                             Database.FavoritesCrud.GetUsersDictionaryAsync(activitiesTargetIds))
                                         .ConfigureAwait(false);
            var rtdicTask = DatabaseUtil.RetryIfLocked(() =>
                                            Database.RetweetsCrud.GetUsersDictionaryAsync(activitiesTargetIds))
                                        .ConfigureAwait(false);

            var addstatus = (await additionalStatusesTask).ToDictionary(d => d.Id);
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
                        if (status.RetweetOriginalId != null || status.QuoteId != null)
                        {
                            TwitterStatus retweet = null;
                            if (status.RetweetOriginalId != null && status.RetweetOriginalUserId != null)
                            {
                                var rtid = status.RetweetOriginalId.Value;
                                var r = addstatus[status.RetweetOriginalId.Value];
                                retweet = Mapper.Map(r, Mapper.Resolve(ses, rtid),
                                    Mapper.Resolve(favdic, rtid), Mapper.Resolve(rtdic, rtid),
                                    users[status.RetweetOriginalUserId.Value]);
                            }
                            TwitterStatus quote = null;
                            if (status.QuoteId != null && status.QuoteUserId != null)
                            {
                                var qid = status.QuoteId.Value;
                                var q = addstatus[status.QuoteId.Value];
                                quote = Mapper.Map(q, Mapper.Resolve(ses, qid),
                                    Mapper.Resolve(favdic, qid), Mapper.Resolve(rtdic, qid),
                                    users[status.QuoteUserId.Value]);
                            }

                            result.Add(Mapper.Map(status, ents, favs, rts, retweet, quote, users[status.UserId]));
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

        #endregion Load from database
    }
}