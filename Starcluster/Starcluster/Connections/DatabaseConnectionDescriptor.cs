using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Starcluster.Connections
{
    /// <summary>
    /// Delegates SQLite database connection.
    /// </summary>
    public sealed class DatabaseConnectionDescriptor : DatabaseConnectionDescriptorBase
    {
        private readonly SqliteConnection _connection;

        public DatabaseConnectionDescriptor(string dbFilePath)
        {
            _connection = new SqliteConnection(ConnectionStringBuilder.CreateWithDataSource(dbFilePath));
            _connection.Open();
            _connection.Execute("PRAGMA case_sensitive_like=1");
        }

        protected override IDbConnection CreateConnectionCore()
        {
            return new SharedDbConnection(_connection);
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