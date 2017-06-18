using System;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models.Entities
{
    public abstract class DbTwitterEntity
    {
        public static DbTwitterEntity ToDatabaseEntity(long parentId, [NotNull] TwitterEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            switch (entity)
            {
                case TwitterUserMentionEntity ume:
                    return new DbUserMentionEntity(parentId, ume);

                case TwitterHashtagEntity he:
                    return new DbTextEntity(parentId, he);

                case TwitterUrlEntity ue:
                    return new DbUrlEntity(parentId, ue);

                case TwitterSymbolEntity se:
                    return new DbTextEntity(parentId, se);

                case TwitterMediaEntity me:
                    return new DbMediaEntity(parentId, me);

                default:
                    throw new ArgumentException("unknown entity type", nameof(entity));
            }
        }

        protected DbTwitterEntity()
        {
        }

        protected DbTwitterEntity(long parentId, TwitterEntity entity)
        {
            ParentId = parentId;
            StartIndex = entity.Indices.Item1;
            EndIndex = entity.Indices.Item2;
        }

        public Tuple<int, int> ToIndices()
        {
            return Tuple.Create(StartIndex, EndIndex);
        }

        [DbPrimaryKey(true)]
        public long Id { get; set; }

        public long ParentId { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }
    }
}