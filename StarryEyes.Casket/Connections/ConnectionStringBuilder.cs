using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Casket.Connections
{
    internal static class ConnectionStringBuilder
    {
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
                {"Default IsolationLevel", "Serializable"},
                {"Journal Mode", "WAL"},
                {"Page Size", "2048"},
                {"Pooling", "True"},
                {"Max Pool Size", "200"},
            };
            return dic.Select(kvp => kvp.Key + "=" + kvp.Value)
                      .JoinString(";");
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
