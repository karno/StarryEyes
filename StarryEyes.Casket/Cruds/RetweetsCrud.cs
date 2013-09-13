using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class RetweetsCrud : ActivityCrudBase<DatabaseRetweet>
    {
        protected override string IndexPrefix
        {
            get { return "RT"; }
        }
    }
}
