using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    public class FilterOperatorEquals : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "=="; }
        }

        protected virtual string SqlOperator
        {
            get { return " = "; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            switch (this.GetArgumentIntersectTypes())
            {
                case FilterExpressionType.Boolean:
                    var lbp = LeftValue.GetBooleanValueProvider();
                    var rbp = RightValue.GetBooleanValueProvider();
                    return _ => lbp(_) == rbp(_);
                case FilterExpressionType.Numeric:
                    var lnp = LeftValue.GetNumericValueProvider();
                    var rnp = RightValue.GetNumericValueProvider();
                    return _ => lnp(_) == rnp(_);
                case FilterExpressionType.String:
                    var lsp = LeftValue.GetStringValueProvider();
                    var rsp = RightValue.GetStringValueProvider();
                    return _ => String.Equals(lsp(_), rsp(_), GetStringComparison());
                default:
                    throw new InvalidOperationException("Invalid code path.");
            }
        }

        public override string GetBooleanSqlQuery()
        {
            Func<FilterOperatorBase, string> converter;
            switch (this.GetArgumentIntersectTypes())
            {
                case FilterExpressionType.Boolean:
                    converter = f => f.GetBooleanSqlQuery();
                    break;
                case FilterExpressionType.Numeric:
                    converter = f => f.GetNumericSqlQuery();
                    break;
                case FilterExpressionType.String:
                    if (this.GetStringComparison() == StringComparison.CurrentCultureIgnoreCase)
                    {
                        converter = f => "LOWER(" + f.GetStringSqlQuery() + ")";
                    }
                    else
                    {
                        converter = f => f.GetStringSqlQuery();
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid code path.");
            }
            return converter(LeftValue) + SqlOperator + converter(RightValue);
        }

        private FilterExpressionType GetArgumentIntersectTypes()
        {
            var supported = LeftValue.SupportedTypes
                                     .Intersect(RightValue.SupportedTypes)
                                     .Except(new[] { FilterExpressionType.Set, })
                                     .ToArray();
            if (supported.Any())
            {
                return supported.First();
            }
            supported = LeftValue.SupportedTypes
                                 .Except(new[] { FilterExpressionType.Set, })
                                 .ToArray();
            if (supported.Any())
            {
                return supported.First();
            }
            // not matched
            throw FilterQueryException.CreateUnsupportedType(
                LeftValue.ToQuery(),
                new[]
                {
                    FilterExpressionType.Boolean,
                    FilterExpressionType.Numeric,
                    FilterExpressionType.String
                },
                this.ToQuery());
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }
    }

    public class FilterOperatorNotEquals : FilterOperatorEquals
    {
        protected override string OperatorString
        {
            get
            {
                return "!=";
            }
        }

        protected override string SqlOperator
        {
            get { return " <> "; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => !base.GetBooleanValueProvider()(_);
        }
    }
}
