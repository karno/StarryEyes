using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class StatusCrud : CrudBase<DatabaseStatus>
    {
        public StatusCrud()
            : base(true)
        {
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("ST_UID", "UserId");
            await this.CreateIndexAsync("ST_ROID", "RetweetOriginalId");
        }

        internal async Task StoreCoreAsync(IEnumerable<Tuple<string, object>> param)
        {
            await this.ExecuteAllAsync(param);
        }

        public Task<IEnumerable<DatabaseStatus>> GetRetweetedStatusesAsync(long originalId)
        {
            return this.QueryAsync<DatabaseStatus>(
                "SELECT * FROM " + this.TableName + " WHERE RetweetedOriginalId = @OriginalId",
                new { OriginalId = originalId });
        }
    }
}
