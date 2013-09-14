using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace StarryEyes.Casket.Infrastructure
{
    [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
    class SqliteRegExp : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            return Regex.IsMatch(Convert.ToString(args[1]),
                                 Convert.ToString(args[0]));
        }
    }
}
