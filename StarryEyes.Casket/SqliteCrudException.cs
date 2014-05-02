using System;
using System.Data.SQLite;

namespace StarryEyes.Casket
{
    public sealed class SqliteCrudException : Exception
    {
        public SQLiteErrorCode ErrorCode
        {
            get { return GetErrorCode(InnerException); }
        }

        public bool IsDatabaseLockedError
        {
            get
            {
                return ErrorCode == SQLiteErrorCode.Locked ||
                       InnerException.Message
                                     .IndexOf("database is locked", StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
        }

        public SqliteCrudException(Exception exception, string tag, string sql)
            : base("Exception has thrown while executing query#" + tag + "(code: " + GetErrorCode(exception) + "): " + sql,
                exception)
        {
        }

        private static SQLiteErrorCode GetErrorCode(Exception exception)
        {
            var sx = exception as SQLiteException;
            return sx == null ? SQLiteErrorCode.Unknown : sx.ResultCode;
        }
    }
}
