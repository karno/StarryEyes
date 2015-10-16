using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class RelationCrudBase : CrudBase<DatabaseRelation>
    {
        internal RelationCrudBase(string tableName) : base(tableName, ResolutionMode.Ignore) { }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor);
            await CreateIndexAsync(TableName + "_IDX_UID", "UserId", false);
        }

        public Task<IEnumerable<DatabaseRelation>> GetAllAsync()
        {
            return Descriptor.QueryAsync<DatabaseRelation>(CreateSql(null), null);
        }

        internal Task DropTableAsync()
        {
            return Descriptor.ExecuteAsync(string.Format("drop table {0};", TableName));
        }

        public async Task<bool> ContainsAsync(long userId, long targetId)
        {
            return (await Descriptor.QueryAsync<DatabaseRelation>(
                CreateSql("UserId = @UserId and TargetId = @TargetId limit 1"),
                new { UserId = userId, TargetId = targetId }).ConfigureAwait(false))
                .SingleOrDefault() != null;
        }

        public Task AddOrUpdateAsync(long userId, long targetId)
        {
            return InsertAsync(new DatabaseRelation(userId, targetId));
        }

        public Task AddOrUpdateAllAsync(long userId, IEnumerable<long> targetIds)
        {
            return Descriptor.ExecuteAllAsync(targetIds.Select(
                id => Tuple.Create(TableInserter, (object)new DatabaseRelation(userId, id))));
        }

        public Task DeleteAsync(long userId, long targetId)
        {
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where UserId = @UserId and TargetId = @TargetId;", TableName),
                new { UserId = userId, TargetId = targetId });
        }

        public Task DeleteAllAsync(long userId, IEnumerable<long> targetId)
        {
            var tids = targetId.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(tids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where UserId = @UserId and TargetId in ({1});", TableName, tids),
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetUsersAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                "select TargetId from " + TableName + " where UserId = @UserId;",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetUsersAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                "select distinct TargetId from " + TableName + ";", null);
        }

        public Task DropUserAsync(long userId)
        {
            return Descriptor.ExecuteAsync(
                string.Format("delete from {0} where UserId = @UserId;", TableName),
                new { UserId = userId });
        }

        public Task DropAllAsync()
        {
            return Descriptor.ExecuteAsync(string.Format("delete from {0};", TableName));
        }
    }

    public class RelationCrud
    {
        private readonly RelationCrudBase _followings;
        private readonly RelationCrudBase _followers;
        private readonly RelationCrudBase _blockings;
        private readonly RelationCrudBase _noRetweets;
        private readonly RelationCrudBase _mutes;

        public RelationCrud()
        {
            _followings = new RelationCrudBase("Followings");
            _followers = new RelationCrudBase("Followers");
            _blockings = new RelationCrudBase("Blockings");
            _noRetweets = new RelationCrudBase("NoRetweets");
            _mutes = new RelationCrudBase("Mutes");
        }

        public Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            return Task.WhenAll(
                _followings.InitializeAsync(descriptor),
                _followers.InitializeAsync(descriptor),
                _blockings.InitializeAsync(descriptor),
                _noRetweets.InitializeAsync(descriptor),
                _mutes.InitializeAsync(descriptor));
        }

        public Task<bool> IsFollowingAsync(long userId, long targetId)
        {
            return _followings.ContainsAsync(userId, targetId);
        }

        public Task<bool> IsFollowerAsync(long userId, long targetId)
        {
            return _followers.ContainsAsync(userId, targetId);
        }

        public Task<bool> IsBlockingAsync(long userId, long targetId)
        {
            return _blockings.ContainsAsync(userId, targetId);
        }

        public Task<bool> IsNoRetweetsAsync(long userId, long targetId)
        {
            return _noRetweets.ContainsAsync(userId, targetId);
        }

        public Task<bool> IsMutedAsync(long userId, long targetId)
        {
            return _mutes.ContainsAsync(userId, targetId);
        }

        public Task SetFollowingAsync(long userId, long targetId, bool following)
        {
            if (following)
            {
                return _followings.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                return _followings.DeleteAsync(userId, targetId);
            }
        }

        public Task SetFollowerAsync(long userId, long targetId, bool followed)
        {
            if (followed)
            {
                return _followers.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                return _followers.DeleteAsync(userId, targetId);
            }
        }

        public Task SetBlockingAsync(long userId, long targetId, bool blocking)
        {
            if (blocking)
            {
                return _blockings.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                return _blockings.DeleteAsync(userId, targetId);
            }
        }

        public Task SetNoRetweetsAsync(long userId, long targetId, bool isSuppressed)
        {
            if (isSuppressed)
            {
                return _noRetweets.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                return _noRetweets.DeleteAsync(userId, targetId);
            }
        }

        public Task SetMutedAsync(long userId, long targetId, bool isMuted)
        {
            if (isMuted)
            {
                return _mutes.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                return _mutes.DeleteAsync(userId, targetId);
            }
        }

        public Task AddFollowingsAsync(long userId, IEnumerable<long> targetIds)
        {
            return _followings.AddOrUpdateAllAsync(userId, targetIds);
        }

        public Task RemoveFollowingsAsync(long userId, IEnumerable<long> removals)
        {
            return _followings.DeleteAllAsync(userId, removals);
        }

        public Task AddFollowersAsync(long userId, IEnumerable<long> targetIds)
        {
            return _followers.AddOrUpdateAllAsync(userId, targetIds);
        }

        public Task RemoveFollowersAsync(long userId, IEnumerable<long> removals)
        {
            return _followers.DeleteAllAsync(userId, removals);
        }

        public Task AddBlockingsAsync(long userId, IEnumerable<long> targetIds)
        {
            return _blockings.AddOrUpdateAllAsync(userId, targetIds);
        }

        public Task RemoveBlockingsAsync(long userId, IEnumerable<long> removals)
        {
            return _blockings.DeleteAllAsync(userId, removals);
        }

        public Task AddNoRetweetsAsync(long userId, IEnumerable<long> targetIds)
        {
            return _noRetweets.AddOrUpdateAllAsync(userId, targetIds);
        }

        public Task RemoveNoRetweetsAsync(long userId, IEnumerable<long> removals)
        {
            return _noRetweets.DeleteAllAsync(userId, removals);
        }

        public Task AddMutesAsync(long userId, IEnumerable<long> targetIds)
        {
            return _mutes.AddOrUpdateAllAsync(userId, targetIds);
        }

        public Task RemoveMutesAsync(long userId, IEnumerable<long> removals)
        {
            return _mutes.DeleteAllAsync(userId, removals);
        }

        public Task DropUserAsync(long userId)
        {
            return Task.Run(() => Task.WaitAll(
                _followings.DropUserAsync(userId),
                _followers.DropUserAsync(userId),
                _blockings.DropUserAsync(userId),
                _noRetweets.DropUserAsync(userId),
                _mutes.DropUserAsync(userId)));
        }

        public Task DropAllAsync()
        {
            return Task.Run(() => Task.WaitAll(
                _followings.DropAllAsync(),
                _followers.DropAllAsync(),
                _blockings.DropAllAsync(),
                _noRetweets.DropAllAsync(),
                _mutes.DropAllAsync()));
        }

        public Task<IEnumerable<long>> GetFollowingsAsync(long userId)
        {
            return _followings.GetUsersAsync(userId);
        }

        public Task<IEnumerable<long>> GetFollowersAsync(long userId)
        {
            return _followers.GetUsersAsync(userId);
        }

        public Task<IEnumerable<long>> GetBlockingsAsync(long userId)
        {
            return _blockings.GetUsersAsync(userId);
        }

        public Task<IEnumerable<long>> GetNoRetweetsAsync(long userId)
        {
            return _noRetweets.GetUsersAsync(userId);
        }

        public Task<IEnumerable<long>> GetMutesAsync(long userId)
        {
            return _mutes.GetUsersAsync(userId);
        }

        public Task<IEnumerable<long>> GetFollowingsAllAsync()
        {
            return _followings.GetUsersAllAsync();
        }

        public Task<IEnumerable<long>> GetFollowersAllAsync()
        {
            return _followers.GetUsersAllAsync();
        }

        public Task<IEnumerable<long>> GetBlockingsAllAsync()
        {
            return _blockings.GetUsersAllAsync();
        }

        public Task<IEnumerable<long>> GetNoRetweetsAllAsync()
        {
            return _noRetweets.GetUsersAllAsync();
        }

        public Task<IEnumerable<long>> GetMutesAllAsync()
        {
            return _mutes.GetUsersAllAsync();
        }
    }
}
