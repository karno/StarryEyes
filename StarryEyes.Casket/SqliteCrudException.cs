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
