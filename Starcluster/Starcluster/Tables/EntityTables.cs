using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcluster.Connections;
using Starcluster.Infrastructures;
using Starcluster.Models;
using Starcluster.Models.Entities;

namespace Starcluster.Tables
{
    public abstract class EntityTableBase<T> : TableBase<T> where T : DbTwitterEntity
    {
        protected EntityTableBase(string tableName) : base(tableName, ResolutionMode.Abort)
        {
        }

        public override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync(TableName + "_PID", nameof(DbTwitterEntity.ParentId), false).ConfigureAwait(false);
        }

        public Task<IEnumerable<T>> GetEntitiesAsync(long parentId)
        {
            return Descriptor.QueryAsync<T>(
                CreateSql(nameof(DbTwitterEntity.ParentId) + " = @Id"),
                new { Id = parentId });
        }

        public async Task<Dictionary<long, IEnumerable<T>>> GetEntitiesDictionaryAsync(IEnumerable<long> parentIds)
        {
            return (await Descriptor.QueryAsync<T>(
                    CreateSql(nameof(DbTwitterEntity.ParentId) + " in @Ids"),
                    new { Ids = parentIds.ToArray() }).ConfigureAwait(false))
                .GroupBy(d => d.ParentId)
                .ToDictionary(d => d.Key, d => d.AsEnumerable());
        }

        public Task DeleteAndInsertAsync(long parentId, IEnumerable<T> entities)
        {
            return Descriptor.ExecuteAllAsync(
                new[] { CreateDeleter(parentId) }
                    .Concat(entities.Select(e => Tuple.Create(TableInserter, (object)e))));
        }

        public async Task DeleteNotExistsAsync(string statusTableName)
        {
            await Descriptor.ExecuteAsync(
                string.Format(
                    "DELETE FROM {0} WHERE NOT EXISTS (SELECT Id FROM {1} " +
                    "WHERE {1}.{2} = {0}.{3});",
                    TableName, statusTableName,
                    nameof(DbTwitterStatus.Id), nameof(DbTwitterEntity.ParentId)));
        }

        public Tuple<string, object> CreateDeleter(long parentId)
        {
            return Tuple.Create($"DELETE FROM {TableName} WHERE {nameof(DbTwitterEntity.ParentId)} = @Id;",
                (object)new { Id = parentId });
        }
    }

    public class MediaEntityTable : EntityTableBase<DbMediaEntity>
    {
        public MediaEntityTable(string tableName) : base(tableName)
        {
        }
    }

    public class TextEntityTable : EntityTableBase<DbTextEntity>
    {
        public TextEntityTable(string tableName) : base(tableName)
        {
        }
    }

    public class UrlEntityTable : EntityTableBase<DbUrlEntity>
    {
        public UrlEntityTable(string tableName) : base(tableName)
        {
        }
    }

    public class UserMentionEntiyTable : EntityTableBase<DbUserMentionEntity>
    {
        public UserMentionEntiyTable(string tableName) : base(tableName)
        {
        }
    }
}