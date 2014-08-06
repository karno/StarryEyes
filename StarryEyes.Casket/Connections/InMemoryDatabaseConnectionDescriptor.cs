using System.Data.Common;
using System.Data.SQLite;
using Dapper;

namespace StarryEyes.Casket.Connections
{
    public class InMemoryDatabaseConnectionDescriptor : DatabaseConnectionDescriptorBase
    {
        private readonly SQLiteConnection _connection;

        public InMemoryDatabaseConnectionDescriptor()
        {
            _connection = new SQLiteConnection(ConnectionStringBuilder.CreateWithFullUri(
                "file::memory:?cache=shared"));
            // opening connection during process is executing
            _connection.Open();
        }

        protected override DbConnection CreateConnectionCore()
        {
            var con = new SQLiteConnection(ConnectionStringBuilder.CreateWithFullUri(
                   "file::memory:?cache=shared"));
            con.Open();
            con.Execute("PRAGMA case_sensitive_like=1");
            return con;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
