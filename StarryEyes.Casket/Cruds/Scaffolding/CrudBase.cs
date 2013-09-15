using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StarryEyes.Albireo.Threading;

namespace StarryEyes.Casket.Cruds.Scaffolding
{
    public abstract class CrudBase
    {
        #region connection string builder

        private static readonly string _baseConStr = CreateBaseConStr();

        private static string CreateBaseConStr()
        {
            var dic = new Dictionary<string, string>
                {
                    {"Version", "3"},
                    {"Cache Size", "8000"},
                    {"Synchronous", "Off"},
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

        #endregion

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
                System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                using (var con = this.OpenConnection())
                using (var tr = con.BeginTransaction())
                {
                    var result = con.Execute(query, transaction: tr);
                    tr.Commit();
                    return result;
                }
            });
        }

        protected Task<int> ExecuteAsync(string query, dynamic param)
        {
            return _factory.StartNew(() =>
            {
                System.Diagnostics.Debug.WriteLine("EXECUTE: " + query);
                using (var con = this.OpenConnection())
                using (var tr = con.BeginTransaction())
                {
                    var result = (int)SqlMapper.Execute(con, query, param, tr);
                    tr.Commit();
                    return result;
                }
            });
        }

        protected Task ExecuteAllAsync(IEnumerable<Tuple<string, object>> queryAndParams)
        {
            return _factory.StartNew(() =>
            {
                using (var con = this.OpenConnection())
                using (var tr = con.BeginTransaction())
                {
                    foreach (var qap in queryAndParams)
                    {
                        System.Diagnostics.Debug.WriteLine("EXECUTE: " + qap.Item1);
                        con.Execute(qap.Item1, qap.Item2, tr);
                    }
                    tr.Commit();
                }
            });
        }

        protected Task<IEnumerable<T>> QueryAsync<T>(string query, object param)
        {
            return _factory.StartNew(() =>
            {
                using (var con = this.OpenConnection())
                {
                    return con.Query<T>(query, param);
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
        {
            this._tableName = SentenceGenerator.GetTableName<T>();
            this._tableCreator = SentenceGenerator.GetTableCreator<T>();
            this._tableInserter = SentenceGenerator.GetTableInserter<T>(onConflict);
            this._tableUpdater = SentenceGenerator.GetTableUpdater<T>();
            this._tableDeleter = SentenceGenerator.GetTableDeleter<T>();
        }

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
                "select * from " + TableName + " where Id = @Id",
                new { Id = key })).SingleOrDefault();
        }

        public virtual async Task InsertOrUpdateAsync(T item)
        {
            await this.ExecuteAsync(this.TableInserter, item);
        }

        public virtual async Task DeleteAsync(long key)
        {
            await this.ExecuteAsync(this.TableDeleter, new { Id = key });
        }
    }
}
