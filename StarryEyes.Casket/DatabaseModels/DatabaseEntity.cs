using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    public abstract class DatabaseEntity : DbModelBase
    {
        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long ParentId { get; set; }

        public EntityType EntityType { get; set; }

        public string DisplayText { get; set; }

        [DbOptional]
        public string OriginalUrl { get; set; }

        public long? UserId { get; set; }

        [DbOptional]
        public string MediaUrl { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }
    }

    [DbName("StatusEntity")]
    public sealed class DatabaseStatusEntity : DatabaseEntity { }

    [DbName("UserDescriptionEntity")]
    public sealed class DatabaseUserDescriptionEntity : DatabaseEntity { }

    [DbName("UserUrlEntity")]
    public sealed class DatabaseUserUrlEntity : DatabaseEntity { }
}
