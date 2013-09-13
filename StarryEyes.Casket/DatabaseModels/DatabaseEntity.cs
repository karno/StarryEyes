using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.DatabaseCore.Sqlite;
using StarryEyes.Casket.DatabaseModels.Generators;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Entity")]
    public class DatabaseEntity : DbModelBase
    {
        public static DatabaseEntity FromTwitterEntity(TwitterEntity entity,
                                                        long parentId, EntityParentType parentType)
        {
            return new DatabaseEntity
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityParentType = parentType,
                EntityType = entity.EntityType,
                MediaUrl = entity.MediaUrl,
                OriginalText = entity.OriginalText,
                ParentId = parentId,
                StartIndex = entity.StartIndex
            };
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long ParentId { get; set; }

        public EntityType EntityType { get; set; }

        public EntityParentType EntityParentType { get; set; }

        public string DisplayText { get; set; }

        public string OriginalText { get; set; }

        [DbOptional]
        public string MediaUrl { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public TwitterEntity ToTwitterEntity()
        {
            return new TwitterEntity
            {
                DisplayText = DisplayText,
                EndIndex = EndIndex,
                EntityType = EntityType,
                MediaUrl = MediaUrl,
                OriginalText = OriginalText,
                StartIndex = StartIndex,
            };
        }
    }

    public enum EntityParentType
    {
        Status,
        UserDescription,
        UserUrl,
    }
}
