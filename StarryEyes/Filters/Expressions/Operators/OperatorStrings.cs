using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cadena.Data;
using StarryEyes.Filters.Expressions.Values.Immediates;
using StarryEyes.Globalization.Filters;

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
        protected override string OperatorString => "startswith";

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
                return h.StartsWith(n, GetStringComparison());
            };
        }

        public override string GetBooleanSqlQuery()
        {
            return GetStringComparison() == StringComparison.CurrentCultureIgnoreCase
                ? "LOWER(" + LeftValue.GetStringSqlQuery() + ") LIKE LOWER('" +
                  RightValue.GetStringSqlQuery().Unwrap() + "%') escape '\\'"
                : LeftValue.GetStringSqlQuery() + " LIKE '" + RightValue.GetStringSqlQuery().Unwrap() +
                  "%' escape '\\'";
        }
    }

    public class OperatorStringEndsWith : FilterTwoValueOperator
    {
        protected override string OperatorString => "endswith";

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
                return h.EndsWith(n, GetStringComparison());
            };
        }

        public override string GetBooleanSqlQuery()
        {
            return GetStringComparison() == StringComparison.CurrentCultureIgnoreCase
                ? "LOWER(" + LeftValue.GetStringSqlQuery() + ") LIKE LOWER('%" +
                  RightValue.GetStringSqlQuery().Unwrap() + "') escape '\\'"
                : LeftValue.GetStringSqlQuery() + " LIKE '%" + RightValue.GetStringSqlQuery().Unwrap() +
                  "' escape '\\'";
        }
    }

    public class OperatorStringRegex : FilterTwoValueOperator
    {
        protected override string OperatorString => "regex";

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            // pre-check regular expressions
            var sv = RightValue as StringValue;
            if (sv != null)
            {
                AssertRegex(sv);
                var haystack = LeftValue.GetStringValueProvider();
                // optimize by pre-compiling
                var needleRegex = new Regex(sv.Value, RegexOptions.Compiled);
                return t =>
                {
                    var h = haystack(t);
                    if (h == null) return false;
                    try
                    {
                        return needleRegex.IsMatch(h);
                    }
                    catch (ArgumentException)
                    {
                        // exception occured
                        return false;
                    }
                };
            }
            else
            {
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
                throw new FilterQueryException(
                    FilterObjectResources.OperatorRegexInvalidPattern +
                    " " + aex.Message, value.ToQuery(), aex);
            }
        }

        public override string GetBooleanSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " REGEXP " + RightValue.GetStringSqlQuery();
        }
    }

    public class OperatorCaseful : FilterSingleValueOperator
    {
        protected override string OperatorString => "caseful";

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return Value.GetStringValueProvider();
        }

        public override string GetStringSqlQuery()
        {
            return Value.GetStringSqlQuery();
        }

        public override string ToQuery()
        {
            return "caseful " + Value.ToQuery();
        }

        public override StringComparison GetStringComparison()
        {
            return StringComparison.CurrentCulture;
        }
    }
}