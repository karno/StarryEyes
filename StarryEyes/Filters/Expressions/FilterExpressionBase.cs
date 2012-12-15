using System;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions
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
            return Operator == null ? "()" : Operator.ToQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            if (Operator == null)
                return _ => true;
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, Operator.SupportedTypes))
                throw new FilterQueryException("Unsupported evaluating as boolean.", Operator.ToQuery());
            return Operator.GetBooleanValueProvider();
        }
    }
}
