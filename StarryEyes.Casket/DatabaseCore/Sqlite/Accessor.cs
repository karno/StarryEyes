using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace StarryEyes.Casket.DatabaseCore.Sqlite
{
    public class Accessor
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

        #region file name resolver

        private static readonly Dictionary<ReferencingTable, string> _files = new Dictionary<ReferencingTable, string>
        {
            {ReferencingTable.Status, "statuses.db"},
            {ReferencingTable.User, "users.db"},
            {ReferencingTable.Entity, "entities,db"},
            {ReferencingTable.Favorites, "favorites.db"},
            {ReferencingTable.Retweets, "retweets.db"}
        };

        private static IEnumerable<string> GetFileNames(ReferencingTable table)
        {
            return _files.Where(file => table.HasFlag(file.Key)).Select(file => file.Value);
        }
        #endregion

        private readonly string[] _filenames;
        private readonly string _constr;

        public Accessor(ReferencingTable tables)
        {
            this._filenames = GetFileNames(tables)
                .Select(p => Path.Combine(Database.BasePath, p))
                .ToArray();
            this._constr = CreateConStr(this._filenames[0]);
        }

        public async Task<int> ExecuteAsync(string query)
        {
            using (var connection = new SQLiteConnection(this._constr))
            {
                await this.OpenDatabaseAsync(connection);
                using (var tr = connection.BeginTransaction())
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.Transaction = tr;
                    return await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, dynamic param)
        {
            using (var connection = new SQLiteConnection(this._constr))
            {
                await this.OpenDatabaseAsync(connection);
                return await SqlMapper.QueryAsync<T>(connection, query, param);
            }
        }

        private async Task OpenDatabaseAsync(SQLiteConnection connection)
        {
            await connection.OpenAsync();
            // attach other databases
            this._filenames.Skip(1)
                      .Select(s => new SQLiteCommand("attach '" + s + "' as " + s.Substring(0, 4), connection))
                      .ForEach(s => s.Using(async c => await c.ExecuteNonQueryAsync()));
        }
    }

    [Flags]
    public enum ReferencingTable
    {
        Status = 1,
        User = 2,
        Entity = 4,
        Favorites = 8,
        Retweets = 16,
    }
}
