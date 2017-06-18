using System.Data;
using Microsoft.Data.Sqlite;

namespace Starcluster.Connections
{
    internal static class ConnectionStringBuilder
    {
        private static readonly string _baseConStr = CreateBaseConStr();

        private static string CreateBaseConStr()
        {
            var builder = new SqliteConnectionStringBuilder
            {
                ["Version"] = "3",
                ["Cache Size"] = "8000",
                ["Cache"] = "Shared",
                ["Synchronous"] = "1",
                ["Default Timeout"] = "3",
                ["Default Isolation Level"] = IsolationLevel.Serializable,
                ["Journal Mode"] = "5", // wal
                ["Page Size"] = "2048",
                ["Pooling"] = "true"
            };
            return builder.ToString();
        }

        public static string CreateWithDataSource(string dbfilepath)
        {
            return "Data Source=" + dbfilepath + ";" + _baseConStr;
        }

        public static string CreateWithFullUri(string uri)
        {
            return "FullUri=" + uri + ";" + _baseConStr;
        }
    }
}