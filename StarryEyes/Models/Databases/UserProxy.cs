using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
                async u => await StoreUsersAsync(u).ConfigureAwait(false));
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

        private static async Task StoreUsersAsync(IEnumerable<TwitterUser> pendingUser)
        {
            var map = pendingUser.Select(Mapper.Map)
                                 .Select(UserInsertBatch.CreateBatch);
            await DatabaseUtil.RetryIfLocked(() => Database.StoreUsers(map)).ConfigureAwait(false);
        }

        public static async Task<TwitterUser> GetUserAsync(long id)
        {
            TwitterUser cached;
            if (_userQueue.TryGetValue(id, out cached))
            {
                return cached;
            }
            var u = await DatabaseUtil.RetryIfLocked(() => Database.UserCrud.GetAsync(id)).ConfigureAwait(false);
            if (u == null) return null;
            var ude = Database.UserDescriptionEntityCrud.GetEntitiesAsync(id);
            var uue = Database.UserUrlEntityCrud.GetEntitiesAsync(id);
            return Mapper.Map(u,
                await DatabaseUtil.RetryIfLocked(() => ude).ConfigureAwait(false),
                await DatabaseUtil.RetryIfLocked(() => uue).ConfigureAwait(false));
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
            var user = await Database.UserCrud.GetAsync(screenName).ConfigureAwait(false);
            return user == null ? null : await LoadUserAsync(user).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            var targets = ids.ToArray();
            if (targets.Length == 0)
            {
                return Enumerable.Empty<TwitterUser>();
            }
            var queued = _userQueue.Find(u => targets.Any(t => t == u.Id)).ToArray();
            var dt = targets.Except(queued.Select(u => u.Id)).ToArray();
            var dbu = await DatabaseUtil.RetryIfLocked(() =>
                Database.UserCrud.GetUsersAsync(dt)).ConfigureAwait(false);
            var resolved = await ResolveUsersAsync(dbu).ConfigureAwait(false);
            return queued.Concat(resolved);
        }

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(string partOfScreenName)
        {
            var dbu = await DatabaseUtil.RetryIfLocked(() =>
                Database.UserCrud.GetUsersAsync(partOfScreenName)).ConfigureAwait(false);
            var resolved = await ResolveUsersAsync(dbu).ConfigureAwait(false);
            return resolved.Concat(_userQueue.Find(
                u => u.ScreenName.IndexOf(partOfScreenName, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetUsersFastAsync(string partOfScreenName, int count)
        {
            var resp = await DatabaseUtil.RetryIfLocked(
                () => Database.UserCrud.GetUsersFastAsync(partOfScreenName, count)).ConfigureAwait(false);
            return resp.Guard().Select(d => Tuple.Create(d.Id, d.ScreenName));
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetRelatedUsersFastAsync(
            string partOfScreenName, bool followingsOnly, int count)
        {
            var resp = await DatabaseUtil.RetryIfLocked(
                () => Database.UserCrud.GetRelatedUsersFastAsync(partOfScreenName, followingsOnly, count)).ConfigureAwait(false);
            return resp.Guard().Select(d => Tuple.Create(d.Id, d.ScreenName));
        }

        private static async Task<IEnumerable<TwitterUser>> ResolveUsersAsync(IEnumerable<DatabaseUser> users)
        {
            var targets = users.ToArray();
            if (targets.Length == 0)
            {
                return Enumerable.Empty<TwitterUser>();
            }
            var ids = targets.Select(u => u.Id).ToArray();
            var desTask = DatabaseUtil.RetryIfLocked(() =>
                Database.UserDescriptionEntityCrud.GetEntitiesDictionaryAsync(ids));
            var uesTask = DatabaseUtil.RetryIfLocked(() =>
                Database.UserUrlEntityCrud.GetEntitiesDictionaryAsync(ids));
            var des = await desTask.ConfigureAwait(false);
            var ues = await uesTask.ConfigureAwait(false);
            return Mapper.MapMany(targets, des, ues);
        }

        private static async Task<TwitterUser> LoadUserAsync([NotNull] DatabaseUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var ude = Database.UserDescriptionEntityCrud.GetEntitiesAsync(user.Id).ConfigureAwait(false);
            var uue = Database.UserUrlEntityCrud.GetEntitiesAsync(user.Id).ConfigureAwait(false);
            return Mapper.Map(user, await ude, await uue);
        }


        public static Task<bool> ContainsAsync(RelationDataType type, long userId, long targetId)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return IsFollowingAsync(userId, targetId);
                case RelationDataType.Follower:
                    return IsFollowerAsync(userId, targetId);
                case RelationDataType.Blocking:
                    return IsBlockingAsync(userId, targetId);
                case RelationDataType.NoRetweets:
                    return IsNoRetweetsAsync(userId, targetId);
                case RelationDataType.Mutes:
                    return IsMutedAsync(userId, targetId);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static async Task<bool> IsFollowingAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.IsFollowingAsync(userId, targetId));
        }

        public static async Task<bool> IsFollowerAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.IsFollowerAsync(userId, targetId));
        }

        public static async Task<bool> IsBlockingAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.IsBlockingAsync(userId, targetId));
        }

        public static async Task<bool> IsNoRetweetsAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.IsNoRetweetsAsync(userId, targetId));
        }

        public static async Task<bool> IsMutedAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.IsMutedAsync(userId, targetId));
        }


        public static Task SetAsync(RelationDataType type, long userId, long targetId, bool value)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return SetFollowingAsync(userId, targetId, value);
                case RelationDataType.Follower:
                    return SetFollowerAsync(userId, targetId, value);
                case RelationDataType.Blocking:
                    return SetBlockingAsync(userId, targetId, value);
                case RelationDataType.NoRetweets:
                    return SetNoRetweetsAsync(userId, targetId, value);
                case RelationDataType.Mutes:
                    return SetMutedAsync(userId, targetId, value);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static async Task SetFollowingAsync(long userId, long targetId, bool following)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.SetFollowingAsync(userId, targetId, following));
        }

        public static async Task SetFollowerAsync(long userId, long targetId, bool followed)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.SetFollowerAsync(userId, targetId, followed));
        }

        public static async Task SetBlockingAsync(long userId, long targetId, bool blocking)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.SetBlockingAsync(userId, targetId, blocking));
        }

        public static async Task SetNoRetweetsAsync(long userId, long targetId, bool suppressing)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.SetNoRetweetsAsync(userId, targetId, suppressing));
        }

        public static async Task SetMutedAsync(long userId, long targetId, bool muted)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.SetMutedAsync(userId, targetId, muted));
        }


        public static Task AddAsync(RelationDataType type, long userId, IEnumerable<long> targetIds)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return AddFollowingsAsync(userId, targetIds);
                case RelationDataType.Follower:
                    return AddFollowersAsync(userId, targetIds);
                case RelationDataType.Blocking:
                    return AddBlockingsAsync(userId, targetIds);
                case RelationDataType.NoRetweets:
                    return AddNoRetweetsAsync(userId, targetIds);
                case RelationDataType.Mutes:
                    return AddMutesAsync(userId, targetIds);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static Task RemoveAsync(RelationDataType type, long userId, IEnumerable<long> removalIds)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return RemoveFollowingsAsync(userId, removalIds);
                case RelationDataType.Follower:
                    return RemoveFollowersAsync(userId, removalIds);
                case RelationDataType.Blocking:
                    return RemoveBlockingsAsync(userId, removalIds);
                case RelationDataType.NoRetweets:
                    return RemoveNoRetweetsAsync(userId, removalIds);
                case RelationDataType.Mutes:
                    return RemoveMutesAsync(userId, removalIds);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }


        public static async Task AddFollowingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.AddFollowingsAsync(userId, targetIds));
        }

        public static async Task RemoveFollowingsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.RemoveFollowingsAsync(userId, removals));
        }


        public static async Task AddFollowersAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.AddFollowersAsync(userId, targetIds));
        }

        public static async Task RemoveFollowersAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.RemoveFollowersAsync(userId, removals));
        }


        public static async Task AddBlockingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.AddBlockingsAsync(userId, targetIds));
        }

        public static async Task RemoveBlockingsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.RemoveBlockingsAsync(userId, removals));
        }


        public static async Task AddNoRetweetsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.AddNoRetweetsAsync(userId, targetIds));
        }

        public static async Task RemoveNoRetweetsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.RemoveNoRetweetsAsync(userId, removals));
        }


        public static async Task AddMutesAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.AddMutesAsync(userId, targetIds));
        }

        public static async Task RemoveMutesAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.RemoveMutesAsync(userId, removals));
        }


        public static Task<IEnumerable<long>> GetAsync(RelationDataType type, long userId)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return GetFollowingsAsync(userId);
                case RelationDataType.Follower:
                    return GetFollowersAsync(userId);
                case RelationDataType.Blocking:
                    return GetBlockingsAsync(userId);
                case RelationDataType.NoRetweets:
                    return GetNoRetweetsAsync(userId);
                case RelationDataType.Mutes:
                    return GetMutesAsync(userId);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static async Task<IEnumerable<long>> GetFollowingsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetFollowingsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetFollowersAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetFollowersAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetBlockingsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetBlockingsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetNoRetweetsAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetNoRetweetsAsync(userId));
        }

        public static async Task<IEnumerable<long>> GetMutesAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetMutesAsync(userId));
        }


        public static Task<IEnumerable<long>> GetAllAsync(RelationDataType type)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return GetFollowingsAllAsync();
                case RelationDataType.Follower:
                    return GetFollowersAllAsync();
                case RelationDataType.Blocking:
                    return GetBlockingsAllAsync();
                case RelationDataType.NoRetweets:
                    return GetNoRetweetsAllAsync();
                case RelationDataType.Mutes:
                    return GetMutesAllAsync();
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static async Task<IEnumerable<long>> GetFollowingsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetFollowingsAllAsync());
        }

        public static async Task<IEnumerable<long>> GetFollowersAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetFollowersAllAsync());
        }

        public static async Task<IEnumerable<long>> GetBlockingsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetBlockingsAllAsync());
        }

        public static async Task<IEnumerable<long>> GetNoRetweetsAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetNoRetweetsAllAsync());
        }

        public static async Task<IEnumerable<long>> GetMutesAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(() =>
                Database.RelationCrud.GetMutesAllAsync());
        }
    }

    /// <summary>
    /// Describe relation types
    /// </summary>
    public enum RelationDataType
    {
        /// <summary>
        /// Following users
        /// </summary>
        Following,

        /// <summary>
        /// Follower users
        /// </summary>
        Follower,

        /// <summary>
        /// Blocking users
        /// </summary>
        Blocking,

        /// <summary>
        /// Retweet suppressed users
        /// </summary>
        NoRetweets,

        /// <summary>
        /// Muted users
        /// </summary>
        Mutes
    }
}
