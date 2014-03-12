using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class RelationCrudBase : CrudBase<DatabaseRelation>
    {
        internal RelationCrudBase(string tableName) : base(tableName, ResolutionMode.Ignore) { }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync(TableName + "_IDX_UID", "UserId", false);
        }

        public async Task<IEnumerable<DatabaseRelation>> GetAllAsync()
        {
            return await this.QueryAsync<DatabaseRelation>(this.CreateSql(null), null);
        }

        internal async Task DropTableAsync()
        {
            await this.ExecuteAsync("drop table " + TableName);
        }

        public async Task<bool> ContainsAsync(long userId, long targetId)
        {
            return
                (await this.QueryAsync<DatabaseRelation>(
                    this.CreateSql("UserId = @UserId and TargetId = @TargetId limit 1"),
                    new { UserId = userId, TargetId = targetId }))
                    .SingleOrDefault() != null;
        }

        public async Task AddOrUpdateAsync(long userId, long targetId)
        {
            await this.InsertAsync(new DatabaseRelation(userId, targetId));
        }

        public async Task AddOrUpdateAllAsync(long userId, IEnumerable<long> targetIds)
        {
            await
                this.ExecuteAllAsync(targetIds.Select(
                    id => Tuple.Create(this.TableInserter, (object)new DatabaseRelation(userId, id))));
        }

        public async Task DeleteAsync(long userId, long targetId)
        {
            await this.ExecuteAsync(
                "delete from " + TableName + " where UserId = @UserId and TargetId = @TargetId;",
                new { UserId = userId, TargetId = targetId });
        }

        public async Task DeleteAllAsync(long userId, IEnumerable<long> targetId)
        {
            var tids = targetId.Select(i => i.ToString(CultureInfo.InvariantCulture)).JoinString(",");
            if (String.IsNullOrEmpty(tids)) return;
            await this.ExecuteAsync(
                "delete from " + TableName + " wherE UserId = @UserId and TargetId in (" + tids + ");",
                new { UserId = userId });
        }

        public async Task<IEnumerable<long>> GetUsersAsync(long userId)
        {
            return (await this.QueryAsync<long>(
                "select TargetId from " + TableName + " where UserId = @UserId;",
                new { UserId = userId }));
        }

        public async Task<IEnumerable<long>> GetUsersAllAsync()
        {
            return (await this.QueryAsync<long>(
                "select distinct TargetId from " + TableName + ";", null));
        }
    }

    public class RelationCrud
    {
        private readonly RelationCrudBase _followings;
        private readonly RelationCrudBase _followers;
        private readonly RelationCrudBase _blockings;
        private readonly RelationCrudBase _noRetweets;

        public RelationCrud()
        {
            _followings = new RelationCrudBase("Followings");
            _followers = new RelationCrudBase("Followers");
            _blockings = new RelationCrudBase("Blockings");
            _noRetweets = new RelationCrudBase("NoRetweets");
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(
                this._followings.InitializeAsync(),
                this._followers.InitializeAsync(),
                this._blockings.InitializeAsync(),
                this._noRetweets.InitializeAsync());
        }

        public async Task<bool> IsFollowingAsync(long userId, long targetId)
        {
            return await _followings.ContainsAsync(userId, targetId);
        }

        public async Task<bool> IsFollowerAsync(long userId, long targetId)
        {
            return await _followers.ContainsAsync(userId, targetId);
        }

        public async Task<bool> IsBlockingAsync(long userId, long targetId)
        {
            return await _blockings.ContainsAsync(userId, targetId);
        }

        public async Task<bool> IsNoRetweetsAsync(long userId, long targetId)
        {
            return await _noRetweets.ContainsAsync(userId, targetId);
        }

        public async Task SetFollowingAsync(long userId, long targetId, bool following)
        {
            if (following)
            {
                await _followings.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                await _followings.DeleteAsync(userId, targetId);
            }
        }

        public async Task SetFollowerAsync(long userId, long targetId, bool followed)
        {
            if (followed)
            {
                await _followers.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                await _followers.DeleteAsync(userId, targetId);
            }
        }

        public async Task SetBlockingAsync(long userId, long targetId, bool blocking)
        {
            if (blocking)
            {
                await _blockings.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                await _blockings.DeleteAsync(userId, targetId);
            }
        }

        public async Task SetNoRetweetsAsync(long userId, long targetId, bool isSuppressed)
        {
            if (isSuppressed)
            {
                await _noRetweets.AddOrUpdateAsync(userId, targetId);
            }
            else
            {
                await _noRetweets.DeleteAsync(userId, targetId);
            }
        }

        public async Task AddFollowingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await _followings.AddOrUpdateAllAsync(userId, targetIds);
        }

        public async Task RemoveFollowingsAsync(long userId, IEnumerable<long> removals)
        {
            await _followings.DeleteAllAsync(userId, removals);
        }

        public async Task AddFollowersAsync(long userId, IEnumerable<long> targetIds)
        {
            await _followers.AddOrUpdateAllAsync(userId, targetIds);
        }

        public async Task RemoveFollowersAsync(long userId, IEnumerable<long> removals)
        {
            await _followers.DeleteAllAsync(userId, removals);
        }

        public async Task AddBlockingsAsync(long userId, IEnumerable<long> targetIds)
        {
            await _blockings.AddOrUpdateAllAsync(userId, targetIds);
        }

        public async Task RemoveBlockingsAsync(long userId, IEnumerable<long> removals)
        {
            await _blockings.DeleteAllAsync(userId, removals);
        }

        public async Task AddNoRetweetsAsync(long userId, IEnumerable<long> targetIds)
        {
            await _noRetweets.AddOrUpdateAllAsync(userId, targetIds);
        }

        public async Task RemoveNoRetweetsAsync(long userId, IEnumerable<long> removals)
        {
            await _noRetweets.DeleteAllAsync(userId, removals);
        }

        public async Task<IEnumerable<long>> GetFollowingsAsync(long userId)
        {
            return await this._followings.GetUsersAsync(userId);
        }

        public async Task<IEnumerable<long>> GetFollowersAsync(long userId)
        {
            return await this._followers.GetUsersAsync(userId);
        }

        public async Task<IEnumerable<long>> GetBlockingsAsync(long userId)
        {
            return await this._blockings.GetUsersAsync(userId);
        }

        public async Task<IEnumerable<long>> GetNoRetweetsAsync(long userId)
        {
            return await _noRetweets.GetUsersAsync(userId);
        }

        public async Task<IEnumerable<long>> GetFollowingsAllAsync()
        {
            return await this._followings.GetUsersAllAsync();
        }

        public async Task<IEnumerable<long>> GetFollowersAllAsync()
        {
            return await this._followers.GetUsersAllAsync();
        }

        public async Task<IEnumerable<long>> GetBlockingsAllAsync()
        {
            return await this._blockings.GetUsersAllAsync();
        }

        public async Task<IEnumerable<long>> GetNoRetweetsAllAsync()
        {
            return await _noRetweets.GetUsersAllAsync();
        }
    }
}
