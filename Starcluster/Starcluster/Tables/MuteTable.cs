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
    public class MuteTable : TableBase<DbTwitterMute>
    {
        public MuteTable(string tableName) : base(tableName, ResolutionMode.Ignore)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor);
            await CreateIndexAsync("UID", nameof(DbTwitterMute.UserId), false);
            await CreateIndexAsync("TID", nameof(DbTwitterMute.TargetId), false);
        }

        public Task<IEnumerable<DbTwitterMute>> GetAllAsync()
        {
            return Descriptor.QueryAsync<DbTwitterMute>(CreateSql(null), null);
        }

        public Task<IEnumerable<long>> GetIdsAsync(long userId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT DISTINCT {nameof(DbTwitterMute.TargetId)} FROM {TableName} WHERE " +
                $"{nameof(DbTwitterMute.UserId)} = @UserId;",
                new { UserId = userId });
        }

        public async Task<IEnumerable<long>> GetIdsAllAsync()
        {
            var mutes = await GetAllAsync().ConfigureAwait(false);
            return mutes.Select(m => m.TargetId).Distinct();
        }

        public async Task<bool> GetAsync(long userId, long targetId)
        {
            return (await Descriptor.QueryAsync<DbTwitterMute>(
                       CreateSql($"{nameof(DbTwitterMute.UserId)} = @UserId AND" +
                                 $" {nameof(DbTwitterMute.TargetId)} = @TargetId LIMIT 1"),
                       new { UserId = userId, TargetId = targetId }).ConfigureAwait(false))
                   .SingleOrDefault() != null;
        }

        public Task SetAsync(long userId, long targetId)
        {
            return InsertAsync(new DbTwitterMute(userId, targetId));
        }

        public Task AddAllAsync(long userId, IEnumerable<long> targetIds)
        {
            return Descriptor.ExecuteAllAsync(targetIds.Select(
                id => Tuple.Create(TableInserter, (object)new DbTwitterMute(userId, id))));
        }

        public Task DeleteAsync(long userId, long targetId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE " +
                $"{nameof(DbTwitterMute.UserId)} = @UserId AND" +
                $"{nameof(DbTwitterMute.TargetId)} = @TargetId;",
                new { UserId = userId, TargetId = targetId });
        }

        public Task DeleteAllAsync(long userId, IEnumerable<long> targetId)
        {
            var tids = String.Join(",", targetId.Select(i => i.ToString(CultureInfo.InvariantCulture)));
            if (String.IsNullOrEmpty(tids)) return Task.Run(() => { });
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE " +
                $"{nameof(DbTwitterMute.UserId)} = @UserId AND " +
                $"{nameof(DbTwitterMute.TargetId)} IN ({tids});",
                new { UserId = userId });
        }

        public Task<IEnumerable<long>> GetUsersAllAsync()
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT DISTINCT {nameof(DbTwitterMute.TargetId)} FROM {TableName};", null);
        }

        public Task DropUserAsync(long userId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterMute.UserId)} = @UserId;",
                new { UserId = userId });
        }

        public Task DropAllAsync()
        {
            return Descriptor.ExecuteAsync($"DELETE FROM {TableName};");
        }
    }
}