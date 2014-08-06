using System.Data.Common;
using System.Data.SQLite;
using Dapper;

namespace StarryEyes.Casket.Connections
{
    /// <summary>
    /// Delegates SQLite database connection.
    /// </summary>
    public sealed class DatabaseConnectionDescriptor : DatabaseConnectionDescriptorBase
    {
        private readonly SQLiteConnection _connection;

        public DatabaseConnectionDescriptor(string dbFilePath)
        {
            _connection = new SQLiteConnection(ConnectionStringBuilder.CreateWithDataSource(dbFilePath));
            _connection.OpenAndReturn().Execute("PRAGMA case_sensitive_like=1");
        }

        protected override DbConnection CreateConnectionCore()
        {
            return new SharedConnection(_connection);
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
