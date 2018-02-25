using System;
using Cadena.Data;
using StarryEyes.Casket;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions
{
    public abstract class FilterExpressionBase
    {
        public const string TautologySql = "1";

        public const string ContradictionSql = "0";

        public static readonly Func<TwitterStatus, bool> Tautology = _ => true;

        public static readonly Func<TwitterStatus, bool> Contradiction = _ => false;

        public event Action InvalidateRequested;

        public abstract string ToQuery();

        public virtual void BeginLifecycle()
        {
        }

        public virtual void EndLifecycle()
        {
        }

        protected void RaiseInvalidateFilter()
        {
            InvalidateRequested?.Invoke();
        }
    }

    public sealed class FilterExpressionRoot : FilterExpressionBase, IMultiplexPredicate<TwitterStatus>
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
                    Value = new FilterBracket()
                }
            };
        }

        private FilterOperatorBase _operator;

        public FilterOperatorBase Operator
        {
            get => _operator;
            set
            {
                if (_operator != null)
                    _operator.InvalidateRequested -= RaiseInvalidateFilter;
                _operator = value;
                if (_operator != null)
                    _operator.InvalidateRequested += RaiseInvalidateFilter;
            }
        }

        public override string ToQuery()
        {
            return Operator == null ? "()" : Operator.ToQuery();
        }

        public string GetSqlQuery()
        {
            return Operator == null ? "1" : Operator.GetBooleanSqlQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            return Operator == null ? Tautology : Operator.GetBooleanValueProvider();
        }

        public override void BeginLifecycle()
        {
            Operator?.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            Operator?.EndLifecycle();
        }
    }
}