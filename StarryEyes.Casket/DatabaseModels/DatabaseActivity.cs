using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbUniqueColumn("StatusId", "UserId")]
    public abstract class DatabaseActivity : DbModelBase
    {
        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long StatusId { get; set; }

        public long UserId { get; set; }
    }

    [DbName("Favorites")]
    public sealed class DatabaseFavorite : DatabaseActivity { }

    [DbName("Retweets")]
    public sealed class DatabaseRetweet : DatabaseActivity { }
}
