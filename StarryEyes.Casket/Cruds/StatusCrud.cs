using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class StatusCrud : CrudBase<DatabaseStatus>
    {
        public StatusCrud()
            : base(ResolutionMode.Ignore)
        {
        }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync("ST_UID", "UserId", false).ConfigureAwait(false);
            await CreateIndexAsync("ST_ROID", "RetweetOriginalId", false).ConfigureAwait(false);
            await CreateIndexAsync("ST_IRSID", "InReplyToStatusId", false).ConfigureAwait(false);
        }

        internal Task StoreCoreAsync(IEnumerable<Tuple<string, object>> param)
        {
            return Descriptor.ExecuteAllAsync(param);
        }

        public async Task<bool> CheckExistsAsync(long id)
        {
            return (await Descriptor.QueryAsync<long>(
                       "select Id from " + TableName + " where Id = @Id;",
                       new { Id = id }).ConfigureAwait(false)).SingleOrDefault() != 0;
        }

        public Task<IEnumerable<DatabaseStatus>> GetStatusesAsync(IEnumerable<long> ids)
        {
            return ids.Chunk(Database.MAX_PARAM_LENGTH)
                      .Select(chunk => Descriptor.QueryAsync<DatabaseStatus>(
                          CreateSql("Id IN @Ids"),
                          new { Ids = chunk.ToArray() }))
                      .GatherSelectMany();
        }

        public Task<IEnumerable<DatabaseStatus>> GetRetweetedStatusesAsync(long originalId)
        {
            return Descriptor.QueryAsync<DatabaseStatus>(
                CreateSql("RetweetOriginalId = @OriginalId"),
                new { OriginalId = originalId });
        }

        public Task<IEnumerable<DatabaseStatus>> FetchAsync(string sql)
        {
            return Descriptor.QueryAsync<DatabaseStatus>(sql, null);
        }

        public Task DeleteOldStatusAsync(int keepStatusCount)
        {
            return Descriptor.ExecuteAsync(
                string.Format(
                    "DELETE FROM {0} WHERE Id IN (SELECT Id FROM {0} ORDER BY CreatedAt DESC LIMIT -1 OFFSET @Offset);",
                    TableName),
                new { Offset = keepStatusCount });
        }

        public Task DeleteOrphanedRetweetAsync()
        {
            return Descriptor.ExecuteAsync(
                string.Format("DELETE FROM {0} WHERE RetweetOriginalid IS NOT NULL AND " +
                              "RetweetOriginalId NOT IN (SELECT Id FROM {0});",
                    TableName));
        }

        public async Task<long?> GetInReplyToAsync(long id)
        {
            return (await Descriptor.QueryAsync<long?>(
                    "select InReplyToStatusId " +
                    "from " + TableName + " " +
                    "where Id = @Id limit 1;", new { Id = id }).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return Descriptor.QueryAsync<long>(
                "select Id " +
                "from " + TableName + " " +
                "where InReplyToStatusId is not null and " +
                "InReplyToStatusId = @inReplyTo;", new { inReplyTo });
        }

        public async Task<long> GetCountAsync()
        {
            return (await Descriptor.QueryAsync<long>("select count(*) from " + TableName + ";", null)
                                    .ConfigureAwait(false)).Single();
        }
    }
}