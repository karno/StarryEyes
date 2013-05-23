using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace StarryEyes.Casket.SQLiteInternal
{
    [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
    internal class SQLiteRegexp : SQLiteFunction
    {
        // TEXT REGEXP PATTERN
        public override object Invoke(object[] args)
        {
            return Regex.IsMatch(Convert.ToString(args[1]), Convert.ToString(args[0]),
                                 RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
