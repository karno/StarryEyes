using System.Data;
using System.Data.Common;

namespace Starcluster.Connections
{
    /// <summary>
    /// Represents connection which ignores dispose method.
    /// </summary>
    public class SharedDbConnection : IDbConnection
    {
        public int ConnectionTimeout => 15;

        private readonly DbConnection _baseConnection;

        public SharedDbConnection(DbConnection baseConnection)
        {
            _baseConnection = baseConnection;
        }

        protected DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _baseConnection.BeginTransaction(isolationLevel);
        }

        public void Close()
        {
            // do nothing.
        }

        public void ChangeDatabase(string databaseName)
        {
            _baseConnection.ChangeDatabase(databaseName);
        }

        public void Open()
        {
            // do nothing.
        }

        public string ConnectionString
        {
            get { return _baseConnection.ConnectionString; }
            set { _baseConnection.ConnectionString = value; }
        }

        public string Database => _baseConnection.Database;

        public ConnectionState State => _baseConnection.State;

        public string DataSource => _baseConnection.DataSource;

        public string ServerVersion => _baseConnection.ServerVersion;

        protected DbCommand CreateDbCommand()
        {
            return _baseConnection.CreateCommand();
        }

        public void Dispose()
        {
            // Do Nothing.
        }

        public IDbTransaction BeginTransaction()
        {
            return BeginDbTransaction(IsolationLevel.Unspecified);
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return BeginDbTransaction(il);
        }

        public IDbCommand CreateCommand()
        {
            return CreateDbCommand();
        }
    }
}