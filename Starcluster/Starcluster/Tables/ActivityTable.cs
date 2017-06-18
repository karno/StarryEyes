using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public class ActivityTable : TableBase<DbTwitterActivity>
    {
        public ActivityTable(string tableName)
            : base(tableName, ResolutionMode.Ignore)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("SID", nameof(DbTwitterActivity.StatusId), false).ConfigureAwait(false);
            await CreateIndexAsync("UID", nameof(DbTwitterActivity.StatusId), false).ConfigureAwait(false);
        }

        public Task<IEnumerable<long>> GetUsersAsync(long statusId)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT UserId FROM {TableName} WHERE {nameof(DbTwitterActivity.StatusId)} = @Id;",
                new { Id = statusId });
        }

        public async Task<Dictionary<long, IEnumerable<long>>> GetUsersDictionaryAsync(
            IEnumerable<long> statusIds)
        {
            return (await Descriptor.QueryAsync<DbTwitterActivity>(
                                        CreateSql("StatusId IN @Ids"), new { Ids = statusIds.ToArray() })
                                    .ConfigureAwait(false))
                .GroupBy(e => e.StatusId)
                .ToDictionary(e => e.Key, e => e.Select(d => d.UserId));
        }

        public Task DeleteNotExistsAsync(string statusTableName)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE NOT EXISTS " +
                $"(SELECT Id FROM {statusTableName} WHERE " +
                $"{statusTableName}.{nameof(DbTwitterStatus.Id)} = {TableName}.{nameof(DbTwitterActivity.StatusId)});");
        }

        public Task InsertAsync(long statusId, long userId)
        {
            return base.InsertAsync(new DbTwitterActivity
            {
                StatusId = statusId,
                UserId = userId
            });
        }

        public Task DeleteAsync(long statusId, long userId)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE " +
                $"{nameof(DbTwitterActivity.StatusId)} = @Sid AND {nameof(DbTwitterActivity.UserId)} = @Uid;",
                new { Sid = statusId, Uid = userId });
        }

        public Task InsertAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var inserters = items.Select(i => Tuple.Create(
                TableInserter,
                (object)new DbTwitterActivity
                {
                    StatusId = i.Item1,
                    UserId = i.Item2
                }));
            return Descriptor.ExecuteAllAsync(inserters);
        }

        public Task DeleteAllAsync(IEnumerable<Tuple<long, long>> items)
        {
            var deleters = items.Select(i => Tuple.Create(
                $"DELETE FROM {TableName} WHERE " +
                $"{nameof(DbTwitterActivity.StatusId)} = @Sid AND {nameof(DbTwitterActivity.UserId)} = @Uid;",
                (object)new { Sid = i.Item1, Uid = i.Item2 }));
            return Descriptor.ExecuteAllAsync(deleters);
        }
    }
}