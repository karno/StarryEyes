using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class UserDescriptionEntityCrud : EntityCrudBase<DatabaseUserDescriptionEntity>
    {
        protected override string IndexPrefix
        {
            get { return "UDET"; }
        }
    }
}
