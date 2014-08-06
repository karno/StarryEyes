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
            await base.InitializeAsync(descriptor);
            await this.CreateIndexAsync(IndexPrefix + "_PID", "ParentId", false);
        }

        public async Task<IEnumerable<T>> GetEntitiesAsync(long parentId)
        {
            return await Descriptor.QueryAsync<T>(
                this.CreateSql("ParentId = @Id"),
                new { Id = parentId });
        }

        public async Task DeleteAndInsertAsync(long parentId, IEnumerable<T> entities)
        {
            await Descriptor.ExecuteAllAsync(
                new[] { this.CreateDeleter(parentId) }
                    .Concat(entities.Select(e => Tuple.Create(this.TableInserter, (object)e))));
        }

        internal Tuple<string, object> CreateDeleter(long parentId)
        {
            return Tuple.Create("delete from " + this.TableName + " where ParentId = @Id;",
                         (object)new { Id = parentId });
        }
    }
}
