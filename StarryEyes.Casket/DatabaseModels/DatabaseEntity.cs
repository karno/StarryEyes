using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.DatabaseCore.Sqlite;
using StarryEyes.Casket.DatabaseModels.Generators;

namespace StarryEyes.Casket.DatabaseModels
{
    public class DatabaseEntity : DbModelBase
    {
        [DbPrimaryKey]
        public long Id { get; set; }

        public long ParentId { get; set; }

        public EntityType EntityType { get; set; }

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
}
