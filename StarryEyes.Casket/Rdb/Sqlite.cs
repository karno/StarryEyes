using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace StarryEyes.Casket.Rdb
{
    public static class Sqlite
    {
        private static string _constr = null;

        private static Dictionary<string, string> CreateConStrDictionary(string dbfilepath)
        {
            return new Dictionary<string, string>
                {
                    {"Data Source", dbfilepath},
                    {"Version", "3"},
                    {"Cache Size", "8000"},
                    {"Synchronous", "Off"},
                    {"Page Size", "2048"},
                    {"Pooling", "True"},
                    {"Max Pool Size", "200"}
                };
        }

        internal static void Initialize(string dbfilepath)
        {
            _constr = CreateConStrDictionary(dbfilepath)
                .Select(kvp => kvp.Key + "=" + kvp.Value)
                .JoinString(";");
        }

        internal static SQLiteConnection CreateConnection()
        {
            if (_constr == null)
            {
                throw new InvalidOperationException("Database system is not initialized.");
            }
            return new SQLiteConnection(_constr);
        }
    }
}
