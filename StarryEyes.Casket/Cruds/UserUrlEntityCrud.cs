using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class UserUrlEntityCrud : EntityCrudBase<DatabaseUserUrlEntity>
    {
        protected override string IndexPrefix
        {
            get { return "UUET"; }
        }
    }
}
