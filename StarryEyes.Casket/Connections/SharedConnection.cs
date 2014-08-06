using System.Data;
using System.Data.Common;

namespace StarryEyes.Casket.Connections
{
    /// <summary>
    /// Represents connection ignoring dispose method.
    /// </summary>
    public class SharedConnection : DbConnection
    {
        private readonly DbConnection _baseConnection;

        public SharedConnection(DbConnection baseConnection)
        {
            this._baseConnection = baseConnection;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _baseConnection.BeginTransaction(isolationLevel);
        }

        public override void Close()
        {
            // do nothing.
        }

        public override void ChangeDatabase(string databaseName)
        {
            _baseConnection.ChangeDatabase(databaseName);
        }

        public override void Open()
        {
            // do nothing.
        }

        public override string ConnectionString
        {
            get { return _baseConnection.ConnectionString; }
            set { _baseConnection.ConnectionString = value; }
        }

        public override string Database
        {
            get { return _baseConnection.Database; }
        }

        public override ConnectionState State
        {
            get { return _baseConnection.State; }
        }

        public override string DataSource
        {
            get { return _baseConnection.DataSource; }
        }

        public override string ServerVersion
        {
            get { return _baseConnection.ServerVersion; }
        }

        protected override DbCommand CreateDbCommand()
        {
            return _baseConnection.CreateCommand();
        }
    }
}
