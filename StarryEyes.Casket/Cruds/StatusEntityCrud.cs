using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class StatusEntityCrud : EntityCrudBase<DatabaseStatusEntity>
    {
        protected override string IndexPrefix
        {
            get { return "SET"; }
        }
    }
}
