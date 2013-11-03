using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions.Values.Immediates;

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

    public class OperatorStringStartsWith : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "startswith"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t =>
            {
                var h = haystack(t);
                var n = needle(t);
                if (h == null || n == null) return false;
                return h.StartsWith(n, StringComparison.CurrentCultureIgnoreCase);
            };
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
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t =>
            {
                var h = haystack(t);
                var n = needle(t);
                if (h == null || n == null) return false;
                return h.EndsWith(n, StringComparison.CurrentCultureIgnoreCase);
            };
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
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            // pre-check regular expressions
            this.AssertRegex(this.RightValue as StringValue);
            var haystack = LeftValue.GetStringValueProvider();
            var needle = RightValue.GetStringValueProvider();
            return t =>
            {
                var h = haystack(t);
                var n = needle(t);
                if (h == null || n == null) return false;
                try
                {
                    return Regex.IsMatch(h, n);
                }
                catch (ArgumentException)
                {
                    // exception occured
                    return false;
                }
            };
        }

        private void AssertRegex(StringValue value)
        {
            if (value == null) return;
            try
            {
                // ReSharper disable ObjectCreationAsStatement
                new Regex(value.Value);
                // ReSharper restore ObjectCreationAsStatement
            }
            catch (ArgumentException aex)
            {
                throw new FilterQueryException("正規表現に誤りがあります: " + aex.Message, value.ToQuery(), aex);
            }
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " REGEXP " + RightValue.GetStringSqlQuery();
        }
    }
}
