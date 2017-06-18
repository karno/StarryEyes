using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Starcluster.Connections
{
    public class InMemoryDatabaseConnectionDescriptor : DatabaseConnectionDescriptorBase
    {
        private readonly SqliteConnection _connection;

        public InMemoryDatabaseConnectionDescriptor()
        {
            _connection = new SqliteConnection(ConnectionStringBuilder.CreateWithFullUri(
                "file::memory:?cache=shared"));
            // opening connection during process is executing
            _connection.Open();
        }

        protected override IDbConnection CreateConnectionCore()
        {
            var con = new SqliteConnection(ConnectionStringBuilder.CreateWithFullUri(
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