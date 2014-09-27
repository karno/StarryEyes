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
            await base.InitializeAsync(descriptor);
            await this.CreateIndexAsync("ST_UID", "UserId", false);
            await this.CreateIndexAsync("ST_ROID", "RetweetOriginalId", false);
            await this.CreateIndexAsync("ST_IRSID", "InReplyToStatusId", false);
        }

        internal async Task StoreCoreAsync(IEnumerable<Tuple<string, object>> param)
        {
            await Descriptor.ExecuteAllAsync(param);
        }

        public async Task<bool> CheckExistsAsync(long id)
        {
            return (await Descriptor.QueryAsync<long>(
                "select Id from " + TableName + " where Id = @Id;",
                new { Id = id }))
                       .SingleOrDefault() != 0;
        }

        public Task<IEnumerable<DatabaseStatus>> GetStatusesAsync(IEnumerable<long> ids)
        {
            return Descriptor.QueryAsync<DatabaseStatus>(
                this.CreateSql("Id IN @Ids"),
                new { Ids = ids });
        }

        public Task<IEnumerable<DatabaseStatus>> GetRetweetedStatusesAsync(long originalId)
        {
            return Descriptor.QueryAsync<DatabaseStatus>(
                this.CreateSql("RetweetOriginalId = @OriginalId"),
                new { OriginalId = originalId });
        }

        public async Task<IEnumerable<DatabaseStatus>> FetchAsync(string sql)
        {
            return await Descriptor.QueryAsync<DatabaseStatus>(sql, null);
        }

        public async Task DeleteOldStatusAsync(int keepStatusCount)
        {
            await Descriptor.ExecuteAsync(
                string.Format(
                    "DELETE FROM {0} WHERE Id IN (SELECT Id FROM {0} ORDER BY CreatedAt DESC LIMIT -1 OFFSET @Offset);",
                    this.TableName),
                new { Offset = keepStatusCount });
        }

        public async Task DeleteOrphanedRetweetAsync()
        {
            await Descriptor.ExecuteAsync(
                string.Format("DELETE FROM {0} WHERE RetweetOriginalid IS NOT NULL AND " +
                              "RetweetOriginalId NOT IN (SELECT Id FROM {0});",
                    this.TableName));
        }

        public async Task<long?> GetInReplyToAsync(long id)
        {
            return (await Descriptor.QueryAsync<long?>(
                "select InReplyToStatusId " +
                "from " + TableName + " " +
                "where Id = @Id limit 1;", new { Id = id }))
                .FirstOrDefault();
        }

        public async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return await Descriptor.QueryAsync<long>(
                "select Id " +
                "from " + TableName + " " +
                "where InReplyToStatusId is not null and " +
                "InReplyToStatusId = @inReplyTo;", new { inReplyTo });
        }

        public async Task<long> GetCountAsync()
        {
            return (await Descriptor.QueryAsync<long>("select count(*) from " + TableName + ";", null))
                .Single();
        }
    }
}
