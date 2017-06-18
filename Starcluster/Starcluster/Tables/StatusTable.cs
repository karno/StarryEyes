using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;

namespace Starcluster.Tables
{
    public sealed class StatusTable : TableBase<DbTwitterStatus>
    {
        public StatusTable(string tableName) : base(tableName, ResolutionMode.Ignore)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateFtsIndexAsync();
            await CreateIndexAsync("UID", nameof(DbTwitterStatus.UserId), false).ConfigureAwait(false);
            await CreateIndexAsync("ROID", nameof(DbTwitterStatus.RetweetedStatusId), false).ConfigureAwait(false);
            await CreateIndexAsync("IRSID", nameof(DbTwitterStatus.InReplyToStatusId), false).ConfigureAwait(false);
            await CreateIndexAsync("IROID", nameof(DbTwitterStatus.InReplyToOrRecipientId), false)
                .ConfigureAwait(false);
        }

        private async Task CreateFtsIndexAsync()
        {
            await Descriptor.ExecuteAsync(
                $"CREATE VIRTUAL TABLE {TableName}_FTS IF NOT EXISTS " +
                "USING fts4(BigramText);").ConfigureAwait(false);
            await Descriptor.ExecuteAsync(
                $"CREATE TRIGGER {TableName}_AIT IF NOT EXISTS AFTER INSERT ON {TableName} BEGIN " +
                $"INSERT INTO {TableName}_FTS (docid, BigramText) VALUES(new.{nameof(DbTwitterStatus.Id)}, new.{nameof(DbTwitterStatus.BigramText)}); " +
                "END;").ConfigureAwait(false);
            await Descriptor.ExecuteAsync(
                $"CREATE TRIGGER {TableName}_BDT IF NOT EXISTS BEFORE DELETE ON {TableName} BEGIN " +
                $"DELETE FROM {TableName}_FTS WHERE docid=old.{nameof(DbTwitterStatus.Id)}; " +
                "END;").ConfigureAwait(false);
            await Descriptor.ExecuteAsync(
                $"CREATE TRIGGER {TableName}_BUT IF NOT EXISTS BEFORE UPDATE ON {TableName} BEGIN " +
                $"DELETE FROM {TableName}_FTS WHERE docid=old.{nameof(DbTwitterStatus.Id)}; " +
                "END;").ConfigureAwait(false);
            await Descriptor.ExecuteAsync(
                $"CREATE TRIGGER {TableName}_AUT IF NOT EXISTS AFTER UPDATE ON {TableName} BEGIN " +
                $"INSERT INTO {TableName}_FTS (docid, path) VALUES(new.{nameof(DbTwitterStatus.Id)}, new.{nameof(DbTwitterStatus.BigramText)})); " +
                "END;").ConfigureAwait(false);
        }

        internal Task StoreCoreAsync(IEnumerable<Tuple<string, object>> param)
        {
            return Descriptor.ExecuteAllAsync(param);
        }

        public async Task<bool> CheckExistsAsync(long id)
        {
            return (await Descriptor.QueryAsync<long>(
                       $"SELECT Id FROM {TableName} WHERE Id = @Id;",
                       new {Id = id}).ConfigureAwait(false)).SingleOrDefault() != 0;
        }

        public Task<IEnumerable<DbTwitterStatus>> GetStatusesAsync(IEnumerable<long> ids)
        {
            return Descriptor.QueryAsync<DbTwitterStatus>(
                CreateSql("Id IN @Ids"),
                new {Ids = ids});
        }

        public Task<IEnumerable<DbTwitterStatus>> GetRetweetedStatusesAsync(long originalId)
        {
            return Descriptor.QueryAsync<DbTwitterStatus>(
                CreateSql($"{nameof(DbTwitterStatus.RetweetedStatusId)} = @OriginalId"),
                new {OriginalId = originalId});
        }

        public Task<IEnumerable<DbTwitterStatus>> FetchAsync(string sql)
        {
            return Descriptor.QueryAsync<DbTwitterStatus>(sql, null);
        }

        public Task DeleteOldStatusAsync(int keepStatusCount)
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE Id IN (SELECT Id FROM {TableName} " +
                $"ORDER BY {nameof(DbTwitterStatus.CreatedAt)} DESC LIMIT -1 OFFSET @Offset);",
                new {Offset = keepStatusCount});
        }

        public Task DeleteOrphanedRetweetAsync()
        {
            return Descriptor.ExecuteAsync(
                $"DELETE FROM {TableName} WHERE {nameof(DbTwitterStatus.RetweetedStatusId)} IS NOT NULL AND " +
                $"{nameof(DbTwitterStatus.RetweetedStatusId)} NOT IN (SELECT Id FROM {TableName});");
        }

        public async Task<long?> GetInReplyToAsync(long id)
        {
            return (await Descriptor.QueryAsync<long?>(
                    $"SELECT {nameof(DbTwitterStatus.InReplyToStatusId)} FROM {TableName} WHERE Id = @Id LIMIT 1;",
                    new {Id = id}).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return Descriptor.QueryAsync<long>(
                $"SELECT Id FROM {TableName} WHERE {nameof(DbTwitterStatus.InReplyToStatusId)} IS NOT NULL AND " +
                $"{nameof(DbTwitterStatus.InReplyToStatusId)} = @inReplyTo;",
                new {inReplyTo});
        }

        public async Task<long> GetCountAsync()
        {
            return (await Descriptor.QueryAsync<long>("SELECT count(*) FROM " + TableName + ";", null)
                                    .ConfigureAwait(false)).Single();
        }
    }
}