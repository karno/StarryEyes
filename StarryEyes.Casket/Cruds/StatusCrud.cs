using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("ST_UID", "UserId", false);
            await this.CreateIndexAsync("ST_ROID", "RetweetOriginalId", false);
        }

        internal async Task StoreCoreAsync(IEnumerable<Tuple<string, object>> param)
        {
            await this.ExecuteAllAsync(param);
        }

        public async Task<bool> CheckExistsAsync(long id)
        {
            return (await this.QueryAsync<long>(
                "select Id from " + TableName + " where Id = @Id;",
                new { Id = id }))
                       .SingleOrDefault() != 0;
        }

        public Task<IEnumerable<DatabaseStatus>> GetRetweetedStatusesAsync(long originalId)
        {
            return this.QueryAsync<DatabaseStatus>(
                this.CreateSql("RetweetOriginalId = @OriginalId"),
                new { OriginalId = originalId });
        }

        public async Task<IEnumerable<DatabaseStatus>> FetchAsync(string sql)
        {
            return await this.QueryAsync<DatabaseStatus>(sql, null);
        }

        public async Task<long?> GetInReplyToAsync(long id)
        {
            return (await this.QueryAsync<long?>(
                "select InReplyToStatusId " +
                "from " + TableName + " " +
                "where Id = @Id limit 1;", new { Id = id }))
                .FirstOrDefault();
        }

        public async Task<IEnumerable<long>> FindFromInReplyToAsync(long inReplyTo)
        {
            return await this.QueryAsync<long>(
                "select Id " +
                "from " + TableName + " " +
                "where InReplyToStatusId is not null and " +
                "InReplyToStatusId = @inReplyTo;", new { inReplyTo });
        }

        public async Task<long> GetCountAsync()
        {
            return (await this.QueryAsync<long>("select count(*) from " + TableName + ";", null))
                .Single();
        }
    }
}
