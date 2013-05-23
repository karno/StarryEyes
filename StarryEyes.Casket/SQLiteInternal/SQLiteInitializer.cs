using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;

namespace StarryEyes.Casket.SQLiteInternal
{
    class SQLiteInitializer
    {
        // parameters
        // default cache size = 2000
        private const int CacheSize = 8000;

        private readonly string _basePath;

        public SQLiteInitializer(string basePath, bool enforceRecreate = false)
        {
            _basePath = basePath;
            if (enforceRecreate && File.Exists(DatabaseFilePath))
            {
                File.Delete(DatabaseFilePath);
            }
            if (!File.Exists(DatabaseFilePath))
            {
                InitializeDatabaseTable();
            }
        }

        private void InitializeDatabaseTable()
        {
            SQLiteConnection.CreateFile(DatabaseFilePath);
            using (var con = BeginConnect())
            {
                con.Open();
                using (var transact = con.BeginTransaction())
                {
                    con.Execute("CREATE TABLE Status( " +
                                ")");
                    transact.Commit();
                }
            }
        }

        public string DatabaseFilePath
        {
            get { return Path.Combine(_basePath, "krile.db"); }
        }

        public string GetIndexFilePath(string indexName)
        {
            return Path.Combine(_basePath, indexName + ".idx");
        }

        public SQLiteConnection BeginConnect()
        {
            var dic = new Dictionary<string, string>
            {
                {"Data Source", DatabaseFilePath},
                {"Cache Size", CacheSize.ToString()},
                {"Foreign Keys", "True"},
                {"Pooling", "True"}, // Pool connections
                {"Synchronous", "Off"}, // improve performance but cause database corruption when crashing OS
            };
            return new SQLiteConnection(dic.Select(p => p.Key + "=" + p.Value).JoinString(";"));
        }
    }
}
