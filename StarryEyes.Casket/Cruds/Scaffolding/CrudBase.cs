using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Albireo.Threading;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    public abstract class CrudBase
    {
        #region Concurrent lock

        private static readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

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
                {"Synchronous", "Normal"},
                {"Default Timeout", "3"},
                {"Journal Mode", "WAL"},
                {"Page Size", "2048"},
                {"Pooling", "True"},
                {"Max Pool Size", "200"},
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

        private static readonly TaskFactory _readTaskFactory = LimitedTaskScheduler.GetTaskFactory(8);
        protected static TaskFactory ReadTaskFactory { get { return _readTaskFactory; } }

        private static readonly TaskFactory _writeTaskFactory = LimitedTaskScheduler.GetTaskFactory(1);
        protected static TaskFactory WriteTaskFactory { get { return _writeTaskFactory; } }

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

        protected SQLiteConnection DangerousOpenConnection()
        {
#if DEBUG
            if (!ReaderWriterLock.IsReadLockHeld &&
                !ReaderWriterLock.IsUpgradeableReadLockHeld &&
                !ReaderWriterLock.IsWriteLockHeld)
            {
                throw new InvalidOperationException("This thread does not have any locks!");
            }
#endif
            SQLiteConnection con = null;
            try
            {
                con = new SQLiteConnection(CreateConStr(Database.DbFilePath));
                con.Open();
                con.Execute("PRAGMA case_sensitive_like=1");
                return con;
            }
            catch (Exception)
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
            return _writeTaskFactory.StartNew(() =>
            {
                // System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    using (var con = this.DangerousOpenConnection())
                    using (var tr = con.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var result = con.Execute(query, transaction: tr);
                        tr.Commit();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    throw WrapException(ex, "ExecuteAsync", query);
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        protected Task<int> ExecuteAsync(string query, dynamic param)
        {
            return _writeTaskFactory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    // System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                    using (var con = this.DangerousOpenConnection())
                    using (var tr = con.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        var result = (int)SqlMapper.Execute(con, query, param, tr);
                        tr.Commit();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    throw WrapException(ex, "ExecuteAsyncWithParam", query);
                }
                finally
                {
                    ReaderWriterLock.ExitWriteLock();
                }
            });
        }

        protected Task ExecuteAllAsync(IEnumerable<Tuple<string, object>> queryAndParams)
        {
            var qnp = queryAndParams.Memoize();
            return _writeTaskFactory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterWriteLock();
                    using (var con = this.DangerousOpenConnection())
                    using (var tr = con.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        foreach (var qap in qnp)
                        {
                            // System.Diagnostics.Debug.WriteLine("EXECUTE: " + qap.Item1);
                            con.Execute(qap.Item1, qap.Item2, tr);
                        }
                        tr.Commit();
                    }
                }
                catch (Exception ex)
                {
                    throw WrapException(ex, "ExecuteAllAsync",
                        qnp.Select(q => q.Item1).JoinString(Environment.NewLine));
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
            return _readTaskFactory.StartNew(() =>
            {
                try
                {
                    ReaderWriterLock.EnterReadLock();
                    using (var con = this.DangerousOpenConnection())
                    {
                        return con.Query<T>(query, param);
                    }
                }
                catch (Exception ex)
                {
                    throw WrapException(ex, "QueryAsync", query);
                }
                finally
                {
                    ReaderWriterLock.ExitReadLock();
                }
            });
        }

        protected SqliteCrudException WrapException(Exception exception, string command, string query)
        {
            return new SqliteCrudException(exception, command, query);
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

        public virtual async Task DeleteAllAsync(IEnumerable<long> key)
        {
            var queries = key.Select(k => Tuple.Create(this.TableDeleter, (object)new { Id = k })).ToArray();
            await this.ExecuteAllAsync(queries);
        }
    }
}
