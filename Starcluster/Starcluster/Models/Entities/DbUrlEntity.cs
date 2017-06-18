using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models.Entities
{
    public class DbUrlEntity : DbTwitterEntity
    {
        public DbUrlEntity()
        {
        }

        public DbUrlEntity(long parentId, TwitterUrlEntity entity) : base(parentId, entity)
        {
            Url = entity.Url;
            DisplayUrl = entity.DisplayUrl;
            ExpandedUrl = entity.ExpandedUrl;
        }

        public TwitterUrlEntity ToUrlEntity()
        {
            return new TwitterUrlEntity(ToIndices(), Url, DisplayUrl, ExpandedUrl);
        }

        [CanBeNull, DbOptional]
        public string Url { get; set; }

        [CanBeNull, DbOptional]
        public string DisplayUrl { get; set; }

        [CanBeNull, DbOptional]
        public string ExpandedUrl { get; set; }
    }
}