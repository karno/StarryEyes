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
                       ? "select * from " + this.TableName + ";"
                       : "select * from " + this.TableName + " where " + whereClause + ";";
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

        internal virtual async Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            // initialize descriptor
            _descriptor = descriptor;
            await Descriptor.ExecuteAsync(TableCreator);
        }

        protected async Task CreateIndexAsync(string indexName, string column, bool unique)
        {
            await Descriptor.ExecuteAsync("CREATE " + (unique ? "UNIQUE " : "") + "INDEX IF NOT EXISTS " +
                               indexName + " ON " + TableName + "(" + column + ")");
        }

        public virtual async Task<T> GetAsync(long key)
        {
            return (await Descriptor.QueryAsync<T>(
                this.CreateSql("Id = @Id"),
                new { Id = key })).SingleOrDefault();
        }

        public virtual async Task InsertAsync(T item)
        {
            await Descriptor.ExecuteAsync(this.TableInserter, item);
        }

        public virtual async Task DeleteAsync(long key)
        {
            await Descriptor.ExecuteAsync(this.TableDeleter, new { Id = key });
        }

        public virtual async Task DeleteAllAsync(IEnumerable<long> key)
        {
            var queries = key.Select(k => Tuple.Create(this.TableDeleter, (object)new { Id = k })).ToArray();
            await Descriptor.ExecuteAllAsync(queries);
        }

        public async Task AlterAsync(string newTableName)
        {
            await Descriptor.ExecuteAsync("ALTER TABLE " + this._tableName + " RENAME TO " + newTableName + ";");
        }
    }
}
