using System;
using System.Data.SQLite;

namespace StarryEyes.Casket
{
    public sealed class SqliteCrudException : Exception
    {
        public SQLiteErrorCode ErrorCode
        {
            get
            {
                var sx = InnerException as SQLiteException;
                return sx == null ? SQLiteErrorCode.Unknown : sx.ResultCode;
            }
        }

        public SqliteCrudException(Exception exception, string tag, string sql)
            : base("Exception has thrown while executing query#" + tag + ": " + sql, exception)
        {
        }
    }
}
