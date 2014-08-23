using System;
using StarryEyes.Albireo;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
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
            this.ReapplyRequested.SafeInvoke();
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

        public string GetSqlQuery()
        {
            return this.Operator == null ? "1" : this.Operator.GetBooleanSqlQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            return this.Operator == null ? Tautology : this.Operator.GetBooleanValueProvider();
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
