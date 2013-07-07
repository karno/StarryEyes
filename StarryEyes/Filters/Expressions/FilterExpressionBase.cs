using System;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions
{
    public abstract class FilterExpressionBase
    {
        public static readonly Func<TwitterStatus, bool> Tautology = _ => true;

        public static readonly Func<TwitterStatus, bool> NonTautology = _ => false;

        public event Action ReapplyRequested;

        public abstract string ToQuery();

        public virtual void BeginLifecycle()
        {
        }

        public virtual void EndLifecycle()
        {
        }

        protected void RaiseReapplyFilter()
        {
            var handler = this.ReapplyRequested;
            if (handler != null) handler();
        }

    }

    public sealed class FilterExpressionRoot : FilterExpressionBase
    {
        public static FilterExpressionRoot GetEmpty(bool tautology)
        {
            if (tautology)
            {
                return new FilterExpressionRoot();
            }
            return new FilterExpressionRoot
            {
                Operator = new FilterNegate
                {
                    Value = new FilterBracket(null)
                }
            };
        }

        private FilterOperatorBase _operator;
        public FilterOperatorBase Operator
        {
            get { return _operator; }
            set
            {
                if (_operator != null)
                    _operator.ReapplyRequested -= this.RaiseReapplyFilter;
                _operator = value;
                if (_operator != null)
                    _operator.ReapplyRequested += this.RaiseReapplyFilter;
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
            if (Operator != null)
            {
                Operator.BeginLifecycle();
            }
        }

        public override void EndLifecycle()
        {
            if (Operator != null)
            {
                Operator.EndLifecycle();
            }
        }

    }
}
