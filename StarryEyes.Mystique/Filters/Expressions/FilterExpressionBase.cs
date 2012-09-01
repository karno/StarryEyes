using System;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Mystique.Filters.Expressions.Operators;

namespace StarryEyes.Mystique.Filters.Expressions
{
    public abstract class FilterExpressionBase
    {
        public abstract string ToQuery();
    }

    public sealed class FilterExpressionRoot : FilterExpressionBase
    {
        public FilterOperatorBase Operator { get; set; }

        public override string ToQuery()
        {
            return Operator.ToQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, Operator.SupportedTypes))
                throw new FilterQueryException("Unsupported evaluating as boolean.", Operator.ToQuery());
            return Operator.GetBooleanValueProvider();
        }
    }
}
