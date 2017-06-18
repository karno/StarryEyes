using System;
using Microsoft.Data.Sqlite;
using Starcluster.Infrastructures;

namespace Starcluster
{
    public sealed class DatabaseAccessException : Exception
    {
        public SqliteErrorCode ErrorCode => GetErrorCode(InnerException);

        public bool IsDatabaseLockedError => CheckDatabaseIsLocked(InnerException);

        public DatabaseAccessException(Exception exception, string tag, string sql)
            : base($"Exception has thrown while executing query#{tag}(code: {GetErrorCode(exception)}): {sql}",
                exception)
        {
        }

        private static bool CheckDatabaseIsLocked(Exception exception)
        {
            var code = GetErrorCode(exception);
            return exception.InnerException != null &&
                   (code == SqliteErrorCode.DatabaseLocked || code == SqliteErrorCode.Busy ||
                    exception.InnerException.Message.IndexOf("database is locked",
                        StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    exception.InnerException.Message.IndexOf("locking protocol",
                        StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private static SqliteErrorCode GetErrorCode(Exception exception)
        {
            var sx = exception as SqliteException;
            return (SqliteErrorCode?)sx?.SqliteErrorCode ?? SqliteErrorCode.Unknown;
        }
    }
}