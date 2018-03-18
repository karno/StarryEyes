using System;
using System.Globalization;
using JetBrains.Annotations;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions.Values
{
    public abstract class ValueBase : FilterOperatorBase
    {
        protected override string OperatorString => ToQuery();

        public override StringComparison GetStringComparison()
        {
            return StringComparison.CurrentCultureIgnoreCase;
        }

        protected static string Coalesce(string sql, [CanBeNull] string defaultValue)
        {
            if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));
            return CoalesceSql(sql, "'" + defaultValue + "'");
        }

        protected static string Coalesce(string sql, long defaultValue)
        {
            return CoalesceSql(sql, defaultValue.ToString(CultureInfo.InvariantCulture));
        }

        private static string CoalesceSql(string sql, [CanBeNull] string defaultSql)
        {
            if (defaultSql == null) throw new ArgumentNullException(nameof(defaultSql));
            return "coalesce(" + sql + ", " + defaultSql + ")";
        }
    }
}