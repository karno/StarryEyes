using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public abstract class ActivityCrudBase<T> : CrudBase<T> where T : DatabaseActivity, new()
    {
        protected ActivityCrudBase()
            : base(ResolutionMode.Ignore)
        {
        }

        protected abstract string IndexPrefix { get; }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync(this.IndexPrefix + "_SID", "StatusId", false);
            await this.CreateIndexAsync(this.IndexPrefix + "_UID", "UserId", false);
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
            await base.InsertOrUpdateAsync(new T
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
