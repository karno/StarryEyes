using System.Threading.Tasks;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class EntityCrud : CrudBase<DatabaseEntity>
    {
        public EntityCrud()
            : base(false)
        {
        }

        internal override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await this.CreateIndexAsync("ET_PID", "ParentId");
        }
    }
}
