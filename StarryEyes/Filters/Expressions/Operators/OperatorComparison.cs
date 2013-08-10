using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Operators
{
    public abstract class FilterComparisonBase : FilterTwoValueOperator
    {
        public sealed override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public sealed override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Numeric,
                LeftValue.SupportedTypes, RightValue.SupportedTypes))
                throw new FilterQueryException("Unsupported type in operator " + OperatorString + ". Both sides must be numeric.",
                    this.ToQuery());
            var l = LeftValue.GetNumericValueProvider();
            var r = RightValue.GetNumericValueProvider();
            return Evaluate(l, r);
        }

        protected abstract Func<TwitterStatus, bool> Evaluate(Func<TwitterStatus, long> left, Func<TwitterStatus, long> right);
    }

    public class FilterOperatorLessThan : FilterComparisonBase
    {
        protected override Func<TwitterStatus, bool> Evaluate(Func<TwitterStatus, long> left, Func<TwitterStatus, long> right)
        {
            return _ => left(_) < right(_);
        }

        protected override string OperatorString
        {
            get { return "<"; }
        }
    }

    public class FilterOperatorLessThanOrEqual : FilterComparisonBase
    {
        protected override Func<TwitterStatus, bool> Evaluate(Func<TwitterStatus, long> left, Func<TwitterStatus, long> right)
        {
            return _ => left(_) <= right(_);
        }

        protected override string OperatorString
        {
            get { return "<="; }
        }
    }

    public class FilterOperatorGreaterThan : FilterComparisonBase
    {
        protected override Func<TwitterStatus, bool> Evaluate(Func<TwitterStatus, long> left, Func<TwitterStatus, long> right)
        {
            return _ => left(_) > right(_);
        }

        protected override string OperatorString
        {
            get { return ">"; }
        }
    }

    public class FilterOperatorGreaterThanOrEqual : FilterComparisonBase
    {
        protected override Func<TwitterStatus, bool> Evaluate(Func<TwitterStatus, long> left, Func<TwitterStatus, long> right)
        {
            return _ => left(_) >= right(_);
        }

        protected override string OperatorString
        {
            get { return ">="; }
        }
    }
}
