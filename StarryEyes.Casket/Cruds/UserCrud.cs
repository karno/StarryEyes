using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class UserCrud : CrudBase<DatabaseUser>
    {
        public UserCrud()
            : base(true)
        {
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("UT_SN", "ScreenName", true);
        }
    }
}
