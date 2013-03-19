using System;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters.Expressions.Operators;

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

        protected void RequestReapplyFilter()
        {
            var handler = OnReapplyRequested;
            if (handler != null) handler();
        }

        public event Action OnReapplyRequested;
    }

    public sealed class FilterExpressionRoot : FilterExpressionBase
    {
        private FilterOperatorBase _operator;
        public FilterOperatorBase Operator
        {
            get { return _operator; }
            set
            {
                if (_operator != null)
                    _operator.OnReapplyRequested -= RequestReapplyFilter;
                _operator = value;
                if (_operator != null)
                    _operator.OnReapplyRequested += RequestReapplyFilter;
            }
        }

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
