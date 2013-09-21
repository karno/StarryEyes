using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public abstract class ActivityCrudBase : CrudBase<DatabaseActivity>
    {
        private readonly string _tableName;

        protected ActivityCrudBase(string tableName)
            : base(tableName, ResolutionMode.Ignore)
        {
            _tableName = tableName;
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync(_tableName + "_SID", "StatusId", false);
            await this.CreateIndexAsync(_tableName + "_UID", "UserId", false);
        }

        public Task<IEnumerable<long>> GetUsersAsync(long statusId)
        {
            return
                this.QueryAsync<long>(
                    "SELECT UserId FROM " + this.TableName + " WHERE StatusId = @Id",
                    new { Id = statusId });
        }

        public async Task InsertAsync(long statusId, long userId)
        {
            await base.InsertAsync(new DatabaseActivity
            {
                StatusId = statusId,
                UserId = userId
            });
        }

        public async Task RemoveWhereAsync(long statusId, long userId)
        {
            await this.ExecuteAsync(
                "DELETE FROM " + this.TableName + " WHERE StatusId = @Sid AND UserId = @Uid",
                new { Sid = statusId, Uid = userId });
        }
    }
}
