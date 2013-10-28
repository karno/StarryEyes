using System;
using StarryEyes.Annotations;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions.Values
{
    public abstract class ValueBase : FilterOperatorBase
    {
        protected override string OperatorString
        {
            get { return this.ToQuery(); }
        }

        protected static string Coalesce(string sql, [NotNull] string defaultValue)
        {
            if (defaultValue == null) throw new ArgumentNullException("defaultValue");
            return CoalesceSql(sql, "'" + defaultValue + "'");
        }

        protected static string Coalesce(string sql, long defaultValue)
        {
            return CoalesceSql(sql, defaultValue.ToString());
        }

        private static string CoalesceSql(string sql, [NotNull] string defaultSql)
        {
            if (defaultSql == null) throw new ArgumentNullException("defaultSql");
            return "coalesce(" + sql + ", " + defaultSql + ")";
        }
    }
}
