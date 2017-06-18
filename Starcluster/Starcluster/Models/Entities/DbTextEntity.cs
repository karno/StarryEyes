using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models.Entities
{
    public class DbTextEntity : DbTwitterEntity
    {
        public DbTextEntity()
        {
        }

        public DbTextEntity(long parentId, TwitterHashtagEntity entity) : base(parentId, entity)
        {
            Text = entity.Text;
        }

        public DbTextEntity(long parentId, TwitterSymbolEntity entity) : base(parentId, entity)
        {
            Text = entity.Text;
        }

        public TwitterHashtagEntity ToHashtagEntity()
        {
            return new TwitterHashtagEntity(ToIndices(), Text);
        }

        public TwitterSymbolEntity ToSymbolEntity()
        {
            return new TwitterSymbolEntity(ToIndices(), Text);
        }

        [CanBeNull, DbOptional]
        public string Text { get; set; }
    }
}