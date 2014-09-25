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

        private static async Task StoreUsersAsync(IEnumerable<TwitterUser> pendingUser)
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

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(IEnumerable<long> ids)
        {
            var targets = ids.ToArray();
            if (targets.Length == 0)
            {
                return Enumerable.Empty<TwitterUser>();
            }
            var queued = _userQueue.Find(u => targets.Any(t => t == u.Id)).ToArray();
            var dt = targets.Except(queued.Select(u => u.Id)).ToArray();
            var dbu = await DatabaseUtil.RetryIfLocked(async () =>
                await Database.UserCrud.GetUsersAsync(dt));
            var resolved = await ResolveUsersAsync(dbu);
            return queued.Concat(resolved);
        }

        public static async Task<IEnumerable<TwitterUser>> GetUsersAsync(string partOfScreenName)
        {
            var dbu = await DatabaseUtil.RetryIfLocked(async () =>
                await Database.UserCrud.GetUsersAsync(partOfScreenName));
            var resolved = await ResolveUsersAsync(dbu);
            return resolved.Concat(_userQueue.Find(
                u => u.ScreenName.IndexOf(partOfScreenName, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetUsersFastAsync(string partOfScreenName, int count)
        {
            var resp = await DatabaseUtil.RetryIfLocked(
                async () => await Database.UserCrud.GetUsersFastAsync(partOfScreenName, count));
            return resp.Guard().Select(d => Tuple.Create(d.Id, d.ScreenName));
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetRelatedUsersFastAsync(
            string partOfScreenName, bool followingsOnly, int count)
        {
            var resp = await DatabaseUtil.RetryIfLocked(
                async () => await Database.UserCrud.GetRelatedUsersFastAsync(partOfScreenName, followingsOnly, count));
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
            var desTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.UserDescriptionEntityCrud.GetEntitiesDictionaryAsync(ids));
            var uesTask = DatabaseUtil.RetryIfLocked(async () =>
                await Database.UserUrlEntityCrud.GetEntitiesDictionaryAsync(ids));
            var des = await desTask;
            var ues = await uesTask;
            return Mapper.MapMany(targets, des, ues);
        }

        private static async Task<TwitterUser> LoadUserAsync([NotNull] DatabaseUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var ude = Database.UserDescriptionEntityCrud.GetEntitiesAsync(user.Id);
            var uue = Database.UserUrlEntityCrud.GetEntitiesAsync(user.Id);
            return Mapper.Map(user, await ude, await uue);
        }


        public static async Task<bool> ContainsAsync(RelationDataType type, long userId, long targetId)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return await IsFollowingAsync(userId, targetId);
                case RelationDataType.Follower:
                    return await IsFollowerAsync(userId, targetId);
                case RelationDataType.Blocking:
                    return await IsBlockingAsync(userId, targetId);
                case RelationDataType.NoRetweets:
                    return await IsNoRetweetsAsync(userId, targetId);
                case RelationDataType.Mutes:
                    return await IsMutedAsync(userId, targetId);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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

        public static async Task<bool> IsMutedAsync(long userId, long targetId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.IsMutedAsync(userId, targetId));
        }


        public static async Task SetAsync(RelationDataType type, long userId, long targetId, bool value)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    await SetFollowingAsync(userId, targetId, value);
                    return;
                case RelationDataType.Follower:
                    await SetFollowerAsync(userId, targetId, value);
                    break;
                case RelationDataType.Blocking:
                    await SetBlockingAsync(userId, targetId, value);
                    break;
                case RelationDataType.NoRetweets:
                    await SetNoRetweetsAsync(userId, targetId, value);
                    break;
                case RelationDataType.Mutes:
                    await SetMutedAsync(userId, targetId, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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

        public static async Task SetMutedAsync(long userId, long targetId, bool muted)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.SetMutedAsync(userId, targetId, muted));
        }


        public static async Task AddAsync(RelationDataType type, long userId, IEnumerable<long> targetIds)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    await AddFollowingsAsync(userId, targetIds);
                    break;
                case RelationDataType.Follower:
                    await AddFollowersAsync(userId, targetIds);
                    break;
                case RelationDataType.Blocking:
                    await AddBlockingsAsync(userId, targetIds);
                    break;
                case RelationDataType.NoRetweets:
                    await AddNoRetweetsAsync(userId, targetIds);
                    break;
                case RelationDataType.Mutes:
                    await AddMutesAsync(userId, targetIds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static async Task RemoveAsync(RelationDataType type, long userId, IEnumerable<long> removalIds)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    await RemoveFollowingsAsync(userId, removalIds);
                    break;
                case RelationDataType.Follower:
                    await RemoveFollowersAsync(userId, removalIds);
                    break;
                case RelationDataType.Blocking:
                    await RemoveBlockingsAsync(userId, removalIds);
                    break;
                case RelationDataType.NoRetweets:
                    await RemoveNoRetweetsAsync(userId, removalIds);
                    break;
                case RelationDataType.Mutes:
                    await RemoveMutesAsync(userId, removalIds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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


        public static async Task AddNoRetweetsAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddNoRetweetsAsync(userId, targetIds));
        }

        public static async Task RemoveNoRetweetsAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveNoRetweetsAsync(userId, removals));
        }


        public static async Task AddMutesAsync(long userId, IEnumerable<long> targetIds)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.AddMutesAsync(userId, targetIds));
        }

        public static async Task RemoveMutesAsync(long userId, IEnumerable<long> removals)
        {
            await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.RemoveMutesAsync(userId, removals));
        }


        public static async Task<IEnumerable<long>> GetAsync(RelationDataType type, long userId)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return await GetFollowingsAsync(userId);
                case RelationDataType.Follower:
                    return await GetFollowersAsync(userId);
                case RelationDataType.Blocking:
                    return await GetBlockingsAsync(userId);
                case RelationDataType.NoRetweets:
                    return await GetNoRetweetsAsync(userId);
                case RelationDataType.Mutes:
                    return await GetMutesAsync(userId);
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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

        public static async Task<IEnumerable<long>> GetMutesAsync(long userId)
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetMutesAsync(userId));
        }


        public static async Task<IEnumerable<long>> GetAllAsync(RelationDataType type)
        {
            switch (type)
            {
                case RelationDataType.Following:
                    return await GetFollowingsAllAsync();
                case RelationDataType.Follower:
                    return await GetFollowersAllAsync();
                case RelationDataType.Blocking:
                    return await GetBlockingsAllAsync();
                case RelationDataType.NoRetweets:
                    return await GetNoRetweetsAllAsync();
                case RelationDataType.Mutes:
                    return await GetMutesAllAsync();
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
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

        public static async Task<IEnumerable<long>> GetMutesAllAsync()
        {
            return await DatabaseUtil.RetryIfLocked(async () =>
                await Database.RelationCrud.GetMutesAllAsync());
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
