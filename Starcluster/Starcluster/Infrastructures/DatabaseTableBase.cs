using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Starcluster.Connections;

namespace Starcluster.Infrastructures
{
    public abstract class DatabaseTableBase<T> where T : class
    {
        private IDatabaseConnectionDescriptor _descriptor;

        [NotNull]
        protected IDatabaseConnectionDescriptor Descriptor
        {
            get
            {
                if (_descriptor == null)
                {
                    throw new InvalidOperationException("CrudBase has not initialized yet: " + GetType().Name);
                }
                return _descriptor;
            }
        }

        protected DatabaseTableBase(ResolutionMode onConflict)
            : this(null, onConflict)
        {
        }

        protected DatabaseTableBase(string tableName, ResolutionMode onConflict)
        {
            TableName = tableName ?? SentenceGenerator.GetTableName<T>();
            TableCreator = SentenceGenerator.GetTableCreator<T>(TableName);
            TableInserter = SentenceGenerator.GetTableInserter<T>(TableName, onConflict);
            TableUpdater = SentenceGenerator.GetTableUpdater<T>(TableName);
            TableDeleter = SentenceGenerator.GetTableDeleter<T>(TableName);
        }

        #region query string builder

        protected string CreateSql(string whereClause)
        {
            return String.IsNullOrEmpty(whereClause)
                ? $"SELECT * FROM {TableName};"
                : $"SELECT * FROM {TableName} WHERE {whereClause};";
        }

        #endregion query string builder

        public string TableName { get; }

        protected virtual string TableCreator { get; }

        protected virtual string TableInserter { get; }

        protected virtual string TableUpdater { get; }

        protected virtual string TableDeleter { get; }

        internal virtual Task InitializeAsync(IDatabaseConnectionDescriptor descriptor)
        {
            // initialize descriptor
            _descriptor = descriptor;
            return Descriptor.ExecuteAsync(TableCreator);
        }

        protected Task CreateIndexAsync(string indexName, string column, bool unique)
        {
            return Descriptor.ExecuteAsync(
                $"CREATE {(unique ? "UNIQUE INDEX" : "INDEX")} IF NOT EXISTS {indexName} ON {TableName}({column})");
        }

        public virtual async Task<T> GetAsync(long key)
        {
            return (await Descriptor.QueryAsync<T>(
                CreateSql("Id = @Id"),
                new { Id = key }).ConfigureAwait(false)).SingleOrDefault();
        }

        public virtual Task InsertAsync(T item)
        {
            return Descriptor.ExecuteAsync(TableInserter, item);
        }

        public virtual Task DeleteAsync(long key)
        {
            return Descriptor.ExecuteAsync(TableDeleter, new { Id = key });
        }

        public virtual Task DeleteAllAsync(IEnumerable<long> key)
        {
            var queries = key.Select(k => Tuple.Create(TableDeleter, (object)new { Id = k })).ToArray();
            return Descriptor.ExecuteAllAsync(queries);
        }

        public Task AlterAsync(string newTableName)
        {
            return Descriptor.ExecuteAsync($"ALTER TABLE {TableName} RENAME TO {newTableName};");
        }
    }
}