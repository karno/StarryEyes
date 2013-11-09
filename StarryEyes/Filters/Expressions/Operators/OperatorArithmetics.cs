using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    public sealed class FilterOperatorPlus : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "+"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set, FilterExpressionType.String, }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            var lsp = LeftValue.GetStringValueProvider();
            var rsp = RightValue.GetStringValueProvider();
            return _ => lsp(_) + rsp(_);
        }

        public override string GetStringSqlQuery()
        {
            return LeftValue.GetStringSqlQuery() + " || " + RightValue.GetStringSqlQuery();
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Union(rsp(_)).ToList();
        }

        public override string GetSetSqlQuery()
        {
            return LeftValue.GetSetSqlQuery() + " union " + RightValue.GetSetSqlQuery();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) + rnp(_);
        }

        public override string GetNumericSqlQuery()
        {
            return LeftValue.GetNumericSqlQuery() + " + " + RightValue.GetNumericSqlQuery();
        }
    }

    public sealed class FilterOperatorMinus : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "-"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Except(rsp(_)).ToList();
        }

        public override string GetSetSqlQuery()
        {
            return LeftValue.GetSetSqlQuery() + " except " + RightValue.GetSetSqlQuery();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) - rnp(_);
        }

        public override string GetNumericSqlQuery()
        {
            return LeftValue.GetNumericSqlQuery() + " - " + RightValue.GetNumericSqlQuery();
        }
    }

    public sealed class FilterOperatorProduct : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "*"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                return new[] { FilterExpressionType.Numeric, FilterExpressionType.Set }
                    .Intersect(LeftValue.SupportedTypes)
                    .Intersect(RightValue.SupportedTypes);
            }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            var lsp = LeftValue.GetSetValueProvider();
            var rsp = RightValue.GetSetValueProvider();
            return _ => lsp(_).Intersect(rsp(_)).ToList();
        }

        public override string GetSetSqlQuery()
        {
            return LeftValue.GetSetSqlQuery() + " intersect " + RightValue.GetSetSqlQuery();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ => lnp(_) * rnp(_);
        }

        public override string GetNumericSqlQuery()
        {
            return LeftValue.GetNumericSqlQuery() + " * " + RightValue.GetNumericSqlQuery();
        }
    }

    public sealed class FilterOperatorDivide : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "/"; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            var lnp = LeftValue.GetNumericValueProvider();
            var rnp = RightValue.GetNumericValueProvider();
            return _ =>
            {
                var divider = rnp(_);
                return divider == 0 ? 0 : lnp(_) / divider;
            };
        }

        public override string GetNumericSqlQuery()
        {
            return LeftValue.GetNumericSqlQuery() + " / " + RightValue.GetNumericSqlQuery();
        }
    }
}
