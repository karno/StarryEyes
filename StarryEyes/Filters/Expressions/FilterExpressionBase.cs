using System;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions
{
    public abstract class FilterExpressionBase
    {
        public static readonly Func<TwitterStatus, bool> Tautology = _ => true;

        public static readonly Func<TwitterStatus, bool> NonTautology = _ => false;

        public abstract string ToQuery();

        public virtual void BeginLifecycle()
        {
        }

        public virtual void EndLifecycle()
        {
        }
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
                return Tautology;
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, Operator.SupportedTypes))
                throw new FilterQueryException("Unsupported evaluating as boolean.", Operator.ToQuery());
            return Operator.GetBooleanValueProvider();
        }

        public override void BeginLifecycle()
        {
            Operator.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            Operator.EndLifecycle();
        }
    }
}
