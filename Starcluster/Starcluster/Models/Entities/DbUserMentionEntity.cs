using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models.Entities
{
    public class DbUserMentionEntity : DbTwitterEntity
    {
        public DbUserMentionEntity()
        {
        }

        public DbUserMentionEntity(long parentId, TwitterUserMentionEntity entity) : base(parentId, entity)
        {
            UserId = entity.Id;
            ScreenName = entity.ScreenName;
            Name = entity.Name;
        }

        public TwitterUserMentionEntity ToUserMentionEntity()
        {
            return new TwitterUserMentionEntity(ToIndices(), UserId, ScreenName, Name);
        }

        public long UserId { get; set; }

        [CanBeNull, DbOptional]
        public string ScreenName { get; set; }

        [CanBeNull, DbOptional]
        public string Name { get; set; }
    }
}