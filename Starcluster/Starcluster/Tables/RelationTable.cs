using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class RelationTable : TableBase<DbTwitterRelation>
    {
        public RelationTable(string tableName) : base(tableName, ResolutionMode.Replace)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor);
            await CreateIndexAsync("UID", nameof(DbTwitterRelation.UserId), false);
            await CreateIndexAsync("TID", nameof(DbTwitterRelation.TargetId), false);
        }

        public Task<IEnumerable<DbTwitterRelation>> GetAllAsync()
        {
            return Descriptor.QueryAsync<DbTwitterRelation>(CreateSql(null), null);
        }

        internal Task DropTableAsync()
        {
            return Descriptor.ExecuteAsync($"DROP TABLE {TableName};");
        }

        public async Task<DbTwitterRelation> GetAsync(long userId, long targetId)
        {
            return (await Descriptor.QueryAsync<DbTwitterRelation>(
                    CreateSql($"{nameof(DbTwitterRelation.UserId)} = @UserId AND" +
                              $" {nameof(DbTwitterRelation.TargetId)} = @TargetId LIMIT 1"),
                    new { UserId = userId, TargetId = targetId }).ConfigureAwait(false))
                .SingleOrDefault();
        }

        public Task AddOrUpdateAsync(DbTwitterRelation relation)
        {
            return InsertAsync(relation);
        }

        public Task AddOrUpdateAllAsync(IEnumerable<DbTwitterRelation> relations)
        {
            return Descriptor.ExecuteAllAsync(relations.Select(
                id => Tuple.Create(TableInserter, (object)id)));
        }

        public Task DeleteAsync(long userId, long targetId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.TargetId)} = @TargetId;",
                new { UserId = userId, TargetId = targetId });
        }

        public Task DeleteAllAsync(long userId, IEnumerable<long> targetIds)
        {
            var tids = String.Join(",", targetIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            if (String.IsNullOrEmpty(tids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.TargetId)} IN ({tids});",
                new { UserId = userId });
        }

        public Task DeleteAllAsync(IEnumerable<long> userIds, long targetId)
        {
            var uids = String.Join(",", userIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            if (String.IsNullOrEmpty(uids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterRelation.TargetId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.UserId)} IN ({uids});",
                new { TargetId = targetId });
        }

        public Task<IEnumerable<long>> GetUsersAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId;",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetUsersAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT DISTINCT {nameof(DbTwitterRelation.TargetId)} FROM {TableName};", null);
        }

        public Task DropUserAsync(long userId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterRelation.UserId)} = @UserId;",
                new { UserId = userId });
        }

        public Task DropAllAsync()
        {
            return Descriptor.ExecuteAsync($"DELETE FROM {TableName};");
        }

        public Task<IEnumerable<long>> GetFollowingsAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following};",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetFollowingsAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following};",
                null);
        }

        public Task<IEnumerable<long>> GetFollowersAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.UserId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.TargetId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following};",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetFollowersAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.UserId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following};",
                null);
        }

        public Task<IEnumerable<long>> GetBlockingsAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Blocking};",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetBlockingsAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Blocking};",
                null);
        }

        public Task<IEnumerable<long>> GetNoRetweetsAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following} AND" +
                $"{nameof(DbTwitterRelation.IsRetweetSuppressed)};",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetNoRetweetsAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT {nameof(DbTwitterRelation.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterRelation.RelationState)} = {(int)RelationState.Following} AND" +
                $"{nameof(DbTwitterRelation.IsRetweetSuppressed)};",
                null);
        }

        public Task SetNoRetweetsAsync(long userId, long targetId, bool suppressing)
        {
            if (suppressing)
            {
                return AddOrUpdateAsync(new DbTwitterRelation(userId, targetId, RelationState.Following, true));
            }
            return Descriptor.ExecuteAsync(
                $"UPDATE OR IGNORE {TableName} SET " +
                $"{nameof(DbTwitterRelation.IsRetweetSuppressed)} = @IsRetweetSuppressed WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.TargetId)} = @TargetId;",
                new { IsRetweetSuppressed = false, UserId = userId, TargetId = targetId });
        }

        public Task SetNoRetweetsAsync(long userId, IEnumerable<long> targetIds, bool suppressing)
        {
            if (suppressing)
            {
                return AddOrUpdateAllAsync(targetIds
                    .Select(id => new DbTwitterRelation(userId, id, RelationState.Following, true)));
            }
            var tids = String.Join(",", targetIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            return Descriptor.ExecuteAsync(
                $"UPDATE OR IGNORE {TableName} SET " +
                $"{nameof(DbTwitterRelation.IsRetweetSuppressed)} = @IsRetweetSuppressed WHERE " +
                $"{nameof(DbTwitterRelation.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterRelation.TargetId)} IN ({tids});",
                new { IsRetweetSuppressed = false, UserId = userId });
        }
    }
}