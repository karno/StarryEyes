using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    public static class StringFilterExtension
    {
        public static string Escape(this string unescaped)
        {
            return unescaped.Replace("'", "''");
        }

        public static string Wrap(this string probablyUnwrapped)
        {
            return "'" + probablyUnwrapped.Unwrap() + "'";
        }

        public static string Unwrap(this string wrapped)
        {
            return wrapped.StartsWith("'") && !wrapped.StartsWith("''")
                       ? wrapped.Substring(1, wrapped.Length - 2)
                       : wrapped;
        }
    }

    public class FilterOperatorStringContains : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "contains"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t => haystack(t).IndexOf(needle(t), StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " LIKE '%" + RightValue.GetStringSqlQuery().Unwrap() + "%' escape '\\'";
        }
    }

    public class OperatorStringStartsWith : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "startswith"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t => haystack(t).StartsWith(needle(t), StringComparison.CurrentCultureIgnoreCase);
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " LIKE '" + RightValue.GetStringSqlQuery().Unwrap() + "%' escape '\\'";
        }
    }

    public class OperatorStringEndsWith : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "endswith"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t => haystack(t).EndsWith(needle(t), StringComparison.CurrentCultureIgnoreCase);
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " LIKE '%" + RightValue.GetStringSqlQuery().Unwrap() + "' escape '\\'";
        }
    }

    public class OperatorStringRegex : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "regex"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t => Regex.IsMatch(haystack(t), needle(t));
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " REGEXP " + RightValue.GetStringSqlQuery();
        }
    }
}
