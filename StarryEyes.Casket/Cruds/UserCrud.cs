using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class UserCrud : CrudBase<DatabaseUser>
    {
        public UserCrud()
            : base(ResolutionMode.Replace)
        {
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("UT_SN", "ScreenName", true);
        }

        public async Task<DatabaseUser> GetAsync(string screenName)
        {
            return (await this.QueryAsync<DatabaseUser>(
                this.CreateSql("ScreenName = @ScreenName limit 1"),
                new { ScreenName = screenName }))
                .SingleOrDefault();
        }

        public async Task<IEnumerable<DatabaseUser>> GetUsersAsync(string partOfScreenName)
        {
            return await this.QueryAsync<DatabaseUser>(
                this.CreateSql("ScreenName like '%" + partOfScreenName + "%'"), null);
        }
    }
}
