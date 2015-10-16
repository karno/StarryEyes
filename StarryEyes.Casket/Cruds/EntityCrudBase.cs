using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Casket.Connections;
using StarryEyes.Casket.Cruds.Scaffolding;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Casket.Cruds
{
    public abstract class EntityCrudBase<T> : CrudBase<T> where T : DatabaseEntity, new()
    {
        protected EntityCrudBase()
            : base(ResolutionMode.Abort)
        {
        }

        protected abstract string IndexPrefix { get; }

        internal override async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            await base.InitializeAsync(descriptor).ConfigureAwait(false);
            await CreateIndexAsync(IndexPrefix + "_PID", "ParentId", false).ConfigureAwait(false);
        }

        public Task<IEnumerable<T>> GetEntitiesAsync(long parentId)
        {
            return Descriptor.QueryAsync<T>(
                CreateSql("ParentId = @Id"),
                new { Id = parentId });
        }

        public async Task<Dictionary<long, IEnumerable<T>>> GetEntitiesDictionaryAsync(IEnumerable<long> parentIds)
        {
            return (await Descriptor.QueryAsync<T>(
                CreateSql("ParentId in @Ids"),
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

        internal Tuple<string, object> CreateDeleter(long parentId)
        {
            return Tuple.Create("delete from " + TableName + " where ParentId = @Id;",
                         (object)new { Id = parentId });
        }
    }
}
