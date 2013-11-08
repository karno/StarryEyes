using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Albireo.Threading;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    public abstract class CrudBase
    {
        #region Concurrent lock

        private static ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        #endregion

        #region connection string builder

        private static readonly string _baseConStr = CreateBaseConStr();

        private static string CreateBaseConStr()
        {
            var dic = new Dictionary<string, string>
                {
                    {"Version", "3"},
                    {"Cache Size", "8000"},
                    // This option would cause damage to database image.
                    // {"Synchronous", "Off"}, 
                    {"Page Size", "2048"},
                    {"Pooling", "True"},
                    {"Max Pool Size", "200"}
                };
            return dic.Select(kvp => kvp.Key + "=" + kvp.Value)
                      .JoinString(";");
        }

        private static string CreateConStr(string dbfilepath)
        {
            return "Data Source=" + dbfilepath + ";" + _baseConStr;
        }

        #endregion

        #region threading

        private static readonly LimitedTaskScheduler _scheduler = new LimitedTaskScheduler(16);
        private static readonly TaskFactory _factory = new TaskFactory(_scheduler);
        protected static TaskFactory QueryTaskFactory { get { return _factory; } }

        #endregion

        protected ReaderWriterLockSlim ReaderWriterLock
        {
            get { return _rwlock; }
        }

        protected IDisposable AcquireWriteLock()
        {
            _rwlock.EnterWriteLock();
            return Disposable.Create(() => _rwlock.ExitWriteLock());
        }

        protected IDisposable AcquireReadLock()
        {
            _rwlock.EnterReadLock();
            return Disposable.Create(() => _rwlock.ExitReadLock());
        }

        protected SQLiteConnection OpenConnection()
        {
            SQLiteConnection con = null;
            try
            {
                con = new SQLiteConnection(CreateConStr(Database.DbFilePath));
                con.Open();
                return con;
            }
            catch (SQLiteException)
            {
                if (con != null)
                {
                    try { con.Dispose(); }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch { }
                    // ReSharper restore EmptyGeneralCatchClause
                }
                throw;
            }
        }

        protected Task<int> ExecuteAsync(string query)
        {
            return _factory.StartNew(() =>
            {
                // System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    using (var con = this.OpenConnection())
                    using (var tr = con.BeginTransaction())
                    {
                        var result = con.Execute(query, transaction: tr);
                        tr.Commit();
                        return result;
                    }
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        protected Task<int> ExecuteAsync(string query, dynamic param)
        {
            return _factory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    // System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                    using (var con = this.OpenConnection())
                    using (var tr = con.BeginTransaction())
                    {
                        var result = (int)SqlMapper.Execute(con, query, param, tr);
                        tr.Commit();
                        return result;
                    }
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        protected Task ExecuteAllAsync(IEnumerable<Tuple<string, object>> queryAndParams)
        {
            return _factory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    using (var con = this.OpenConnection())
                    using (var tr = con.BeginTransaction())
                    {
                        foreach (var qap in queryAndParams)
                        {
                            // System.Diagnostics.Debug.WriteLine("EXECUTE: " + qap.Item1);
                            con.Execute(qap.Item1, qap.Item2, tr);
                        }
                        tr.Commit();
                    }
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        protected Task<IEnumerable<T>> QueryAsync<T>(string query, object param)
        {
            // System.Diagnostics.Debug.WriteLine("QUERY: " + query);
            return _factory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterReadLock();
                    using (var con = this.OpenConnection())
                    {
                        return con.Query<T>(query, param);
                    }
                }
                finally
                {
                    ReaderWriterLock.ExitReadLock();
                }
            });
        }
    }

    public abstract class CrudBase<T> : CrudBase where T : class
    {
        private readonly string _tableName;
        private readonly string _tableCreator;
        private readonly string _tableInserter;
        private readonly string _tableUpdater;
        private readonly string _tableDeleter;

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

        internal virtual async Task InitializeAsync()
        {
            await this.ExecuteAsync(TableCreator);
        }

        protected async Task CreateIndexAsync(string indexName, string column, bool unique)
        {
            await this.ExecuteAsync("CREATE " + (unique ? "UNIQUE " : "") + "INDEX IF NOT EXISTS " +
                                    indexName + " ON " + TableName + "(" + column + ")");
        }

        public virtual async Task<T> GetAsync(long key)
        {
            return (await this.QueryAsync<T>(
                this.CreateSql("Id = @Id"),
                new { Id = key })).SingleOrDefault();
        }

        public virtual async Task InsertAsync(T item)
        {
            await this.ExecuteAsync(this.TableInserter, item);
        }

        public virtual async Task DeleteAsync(long key)
        {
            await this.ExecuteAsync(this.TableDeleter, new { Id = key });
        }
    }

    public abstract class CachedCrudBase<T> : CrudBase<T> where T : class
    {
        protected CachedCrudBase(ResolutionMode onConflict)
            : base(onConflict)
        {
            this.ConnectEntrant();
        }

        protected CachedCrudBase(string tableName, ResolutionMode onConflict)
            : base(tableName, onConflict)
        {
            this.ConnectEntrant();
        }

        private readonly Subject<T> _entrant = new Subject<T>();

        protected abstract int DelayMilliSec { get; }

        private void ConnectEntrant()
        {
            _entrant.Buffer(TimeSpan.FromMilliseconds(DelayMilliSec))
                    .Where(l => l.Count > 0)
                    .Subscribe(this.CyclicWriteback);
        }

        private async void CyclicWriteback(IEnumerable<T> item)
        {
            var array = item.Select(i => Tuple.Create(this.TableInserter, (object)i)).ToArray();
            System.Diagnostics.Debug.WriteLine("Write " + array.Length + " items in one batch...");
            await this.ExecuteAllAsync(array);
        }

#pragma warning disable 1998
#pragma warning disable 4014
        public override async Task InsertAsync(T item)
        {
            Task.Run(() => _entrant.OnNext(item));
        }
#pragma warning restore 1998
#pragma warning restore 4014
    }
}
