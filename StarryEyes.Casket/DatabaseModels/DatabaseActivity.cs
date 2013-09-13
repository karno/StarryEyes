using StarryEyes.Casket.DatabaseModels.Generators;
using StarryEyes.Casket.Scaffolds.Generators;

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
    public class DatabaseFavorite : DatabaseActivity { }

    [DbName("Retweets")]
    public class DatabaseRetweet : DatabaseActivity { }
}
