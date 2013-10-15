using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Databases;

namespace StarryEyes.Filters.Expressions.Operators
{
    /// <summary>
    /// Contains as member
    /// </summary>
    public class FilterOperatorContains : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "contains"; }
        }

        private bool CompareAsString()
        {
            // lv: string -> string
            // lv: set|string -> rv: string -> string
            var lst = LeftValue.SupportedTypes.Memoize();
            var rst = RightValue.SupportedTypes.Memoize();
            return !lst.Contains(FilterExpressionType.Set) ||
                   (lst.Contains(FilterExpressionType.String) &&
                    !rst.Contains(FilterExpressionType.Set) && !rst.Contains(FilterExpressionType.Numeric));
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (this.CompareAsString())
            {
                var haystack = this.LeftValue.GetStringValueProvider();
                var needle = this.RightValue.GetStringValueProvider();
                return t =>
                {
                    var h = haystack(t);
                    var n = needle(t);
                    if (h == null || n == null) return false;
                    return h.IndexOf(n, StringComparison.CurrentCultureIgnoreCase) >= 0;
                };
            }
            var lsp = this.LeftValue.GetSetValueProvider();
            if (this.RightValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var rnp = this.RightValue.GetNumericValueProvider();
                return _ => lsp(_).Contains(rnp(_));
            }
            var rsp = this.RightValue.GetSetValueProvider();
            return _ =>
            {
                var ls = lsp(_);
                return rsp(_).Any(ls.Contains);
            };
        }

        public override string GetBooleanSqlQuery()
        {
            if (this.CompareAsString())
            {
                return this.LeftValue.GetStringSqlQuery() + " LIKE '%" +
                       this.RightValue.GetStringSqlQuery().Unwrap() +
                       "%' escape '\\'";
            }
            var lq = this.LeftValue.GetSetSqlQuery();
            if (this.RightValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                return this.RightValue.GetNumericSqlQuery() + " IN " + lq;
            }
            var rq = this.RightValue.GetSetSqlQuery();
            // check intersection
            return "exists (" + lq.Unparenthesis() + " intersect " + rq.Unparenthesis() + ")";
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    /// <summary>
    /// Contained as member
    /// </summary>
    public class FilterOperatorContainedBy : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "in"; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var rsp = RightValue.GetSetValueProvider();
            if (LeftValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                var lnp = LeftValue.GetNumericValueProvider();
                return _ => rsp(_).Contains(lnp(_));
            }
            var lsp = LeftValue.GetSetValueProvider();
            return _ =>
            {
                var rs = rsp(_);
                return lsp(_).Any(rs.Contains);
            };
        }

        public override string GetBooleanSqlQuery()
        {
            var rq = RightValue.GetSetSqlQuery();
            if (LeftValue.SupportedTypes.Contains(FilterExpressionType.Numeric))
            {
                return LeftValue.GetNumericSqlQuery() + " IN " + rq;
            }
            var lq = LeftValue.GetSetSqlQuery();
            // check intersection
            return "exists (" + lq.Unparenthesis() + " intersect " + rq.Unparenthesis() + ")";
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }
}
