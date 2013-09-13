using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public sealed class FavoritesCrud : ActivityCrudBase<DatabaseFavorite>
    {
        protected override string IndexPrefix
        {
            get { return "FT"; }
        }
    }
}
