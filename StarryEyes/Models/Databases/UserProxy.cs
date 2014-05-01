using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Models.Databases.Caching;

namespace StarryEyes.Models.Databases
{
    public static class UserProxy
    {
        private static readonly TaskQueue<long, TwitterUser> _userQueue;

        static UserProxy()
        {
            _userQueue = new TaskQueue<long, TwitterUser>(50, TimeSpan.FromSeconds(30),
                async u => await StoreUsersAsync(u));
            App.ApplicationFinalize += () => _userQueue.Writeback();
        }

        public static long GetId(string screenName)
        {
            var incache = _userQueue
                .Find(u => u.ScreenName.Equals(screenName,
                    StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
            if (incache != null)
            {
                return incache.Id;
            }
            while (true)
            {
                try
                {
                    return Database.UserCrud.GetId(screenName);
                }
                catch (SQLiteException sqex)
                {
                    if (sqex.ResultCode != SQLiteErrorCode.Locked)
                    {
                        throw;
                    }
                }
                catch (SqliteCrudException cex)
                {
                    if (cex.ErrorCode != SQLiteErrorCode.Locked)
                    {
                        throw;
                    }
                }
            }
        }

        public static void StoreUser(TwitterUser user)
        {
            _userQueue.Enqueue(user.Id, user);
        }

        public static async Task StoreUsersAsync(IEnumerable<TwitterUser> pendingUser)
        {
            var map = pendingUser.Select(Mapper.Map)
                                 .Select(UserInsertBatch.CreateBatch);
            await DatabaseUtil.RetryIfLocked(async () => await Database.StoreUsers(map));
        }

        public static async Task<TwitterUser> GetUserAsync(long id)
        {
            TwitterUser cached;
            if (_userQueue.TryGetValue(id, out cached))
            {
                return cached;
            }
            var u = await DatabaseUtil.RetryIfLocked(async () => await Database.UserCrud.GetAsync(id));
            if (u == null) return null;
            var ude = Database.UserDescriptionEntityCrud.GetEntitiesAsync(id);
            var uue = Database.UserUrlEntityCrud.GetEntitiesAsync(id);
            return Mapper.Map(u,
                await DatabaseUtil.RetryIfLocked(async () => await ude),
                await DatabaseUtil.RetryIfLocked(async () => await uue));
        }

        public static async Task<TwitterUser> GetUserAsync(string screenName)
        {
            var incache = _userQueue
                .Find(u => u.ScreenName.Equals(screenName,
                    StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
            if (incache != null)
            {
                return incache;
            }
            var user = await Database.UserCrud.GetAsync(screenName);
            return user == null ? null : await LoadUserAsync(user);
        }

        public static async Task<IObservable<TwitterUser>> GetUsersAsync(string partOfScreenName)
        {
            var dbu = await DatabaseUtil.RetryIfLocked(async () =>
                await Database.UserCrud.GetUsersAsync(partOfScreenName));
            return LoadUsersAsync(dbu).Concat(
                _userQueue.Find(u => u.ScreenName
                                      .IndexOf(partOfScreenName, StringComparison.CurrentCultureIgnoreCase) >= 0)
                          .ToObservable());
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetUsersFastAsync(string partOfScreenName, int count)
        {
            var resp = await DatabaseUtil.RetryIfLocked(
                async () => await Database.UserCrud.GetUsersFastAsync(partOfScreenName, count));
            return resp.Guard().Select(d => Tuple.Create(d.Id, d.ScreenName));
        }

        public static IObservable<TwitterUser> LoadUsersAsync([NotNull] IEnumerable<DatabaseUser> dbusers)
        {
            if (dbusers == null) throw new ArgumentNullException("dbusers");
            return dbusers
                .ToObservable()
                .SelectMany(s => LoadUserAsync(s).ToObservable());
        }

        private static async Task<TwitterUser> LoadUserAsync([NotNull] DatabaseUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var ude = Database.UserDescriptionEntityCrud.GetEntitiesAsync(user.Id);
            var uue = Database.UserUrlEntityCrud.GetEntitiesAsync(user.Id);
            return Mapper.Map(user, await ude, await uue);
        }

        public static async Task<bool> IsFollowingAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.IsFollowingAsync(userId, targetId));
        }

        public static async Task<bool> IsFollowerAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.IsFollowerAsync(userId, targetId));
        }

        public static async Task<bool> IsBlockingAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.IsBlockingAsync(userId, targetId));
        }

        public static async Task<bool> IsNoRetweetsAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.IsNoRetweetsAsync(userId, targetId));
        }

        public static async Task SetFollowingAsync(long userId, long targetId, bool following)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.SetFollowingAsync(userId, targetId, following));
        }

        public static async Task SetFollowerAsync(long userId, long targetId, bool followed)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.SetFollowerAsync(userId, targetId, followed));
        }

        public static async Task SetBlockingAsync(long userId, long targetId, bool blocking)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.SetBlockingAsync(userId, targetId, blocking));
        }

        public static async Task SetNoRetweetsAsync(long userId, long targetId, bool suppressing)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.SetNoRetweetsAsync(userId, targetId, suppressing));
        }

        public static async Task AddFollowingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddFollowingsAsync(userId, targetIds));
        }

        public static async Task RemoveFollowingsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveFollowingsAsync(userId, removals));
        }

        public static async Task AddFollowersAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddFollowersAsync(userId, targetIds));
        }

        public static async Task RemoveFollowersAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveFollowersAsync(userId, removals));
        }

        public static async Task AddBlockingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddBlockingsAsync(userId, targetIds));
        }

        public static async Task RemoveBlockingsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveBlockingsAsync(userId, removals));
        }

        public static async Task AddNoRetweetssAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddNoRetweetsAsync(userId, targetIds));
        }

        public static async Task RemoveNoRetweetssAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveNoRetweetsAsync(userId, removals));
        }

        public static async Task<IEnumerable<long>> GetFollowingsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetFollowingsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetFollowersAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetFollowersAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetBlockingsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetBlockingsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetNoRetweetsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetNoRetweetsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetFollowingsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetFollowingsAllAsync());
        }

        public static async Task<IEnumerable<long>> GetFollowersAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetFollowersAllAsync());
        }

        public static async Task<IEnumerable<long>> GetBlockingsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetBlockingsAllAsync());
        }

        public static async Task<IEnumerable<long>> GetNoRetweetsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetNoRetweetsAllAsync());
        }

    }
}
