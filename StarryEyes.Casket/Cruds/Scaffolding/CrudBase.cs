using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Casket.Connections;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    public abstract class CrudBase<T> where T : class
    {
        private readonly string _tableName;
        private readonly string _tableCreator;
        private readonly string _tableInserter;
        private readonly string _tableUpdater;
        private readonly string _tableDeleter;

        private IDatabaseConnectionDescriptor _descriptor;

        [NotNull]
        protected IDatabaseConnectionDescriptor Descriptor
        {
            get
            {
                if (_descriptor == null)
                {
                    throw new InvalidOperationException("CrudBase has not initialized yet: " + this.GetType().Name);
                }
                return _descriptor;
            }
        }

        public CrudBase(ResolutionMode onConflict)
            : this(null, onConflict)
        {
        }

        public CrudBase(string tableName, ResolutionMode onConflict)
        {
            this._tableName = tableName ?? SentenceGenerator.GetTableName<T>();
            this._tableCreator = SentenceGenerator.GetTableCreator<T>(this._tableName);
            this._tableInserter = SentenceGenerator.GetTableInserter<T>(this._tableName, onConflict);
            this._tableUpdater = SentenceGenerator.GetTableUpdater<T>(this._tableName);
            this._tableDeleter = SentenceGenerator.GetTableDeleter<T>(this._tableName);
        }

        #region query string builder

        protected string CreateSql(string whereClause)
        {
            return String.IsNullOrEmpty(whereClause)
                ? string.Format("select * from {0};", this.TableName)
                : string.Format("select * from {0} where {1};", this.TableName, whereClause);
        }

        #endregion

        public virtual string TableName
        {
            get { return this._tableName; }
        }

        protected virtual string TableCreator
        {
            get { return this._tableCreator; }
        }

        protected virtual string TableInserter
        {
            get { return this._tableInserter; }
        }

        protected virtual string TableUpdater
        {
            get { return this._tableUpdater; }
        }

        protected virtual string TableDeleter
        {
            get { return this._tableDeleter; }
        }

        internal virtual Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            // initialize descriptor
            _descriptor = descriptor;
            return Descriptor.ExecuteAsync(TableCreator);
        }

        protected Task CreateIndexAsync(string indexName, string column, bool unique)
        {
            return Descriptor.ExecuteAsync(string.Format("CREATE {0} IF NOT EXISTS {1} ON {2}({3})",
                    (unique ? "UNIQUE INDEX" : "INDEX"), indexName, this.TableName, column));
        }

        public virtual async Task<T> GetAsync(long key)
        {
            return (await Descriptor.QueryAsync<T>(
                this.CreateSql("Id = @Id"),
                new { Id = key }).ConfigureAwait(false)).SingleOrDefault();
        }

        public virtual Task InsertAsync(T item)
        {
            return Descriptor.ExecuteAsync(this.TableInserter, item);
        }

        public virtual Task DeleteAsync(long key)
        {
            return Descriptor.ExecuteAsync(this.TableDeleter, new { Id = key });
        }

        public virtual Task DeleteAllAsync(IEnumerable<long> key)
        {
            var queries = key.Select(k => Tuple.Create(this.TableDeleter, (object)new { Id = k })).ToArray();
            return Descriptor.ExecuteAllAsync(queries);
        }

        public Task AlterAsync(string newTableName)
        {
            return Descriptor.ExecuteAsync(string.Format("ALTER TABLE {0} RENAME TO {1};",
                _tableName, newTableName));
        }
    }
}
