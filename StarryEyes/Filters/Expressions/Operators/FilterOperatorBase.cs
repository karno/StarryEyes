using System;
using System.Collections.Generic;
using Cadena.Data;

namespace StarryEyes.Filters.Expressions.Operators
{
    // ReSharper disable MemberCanBeProtected.Global
    public abstract class FilterOperatorBase : FilterExpressionBase
    {
        protected abstract string OperatorString { get; }

        public abstract IEnumerable<FilterExpressionType> SupportedTypes { get; }

        public virtual Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            ThrowMismatchType(FilterExpressionType.Boolean);
            return null; //unreachable code
        }

        public virtual Func<TwitterStatus, long> GetNumericValueProvider()
        {
            ThrowMismatchType(FilterExpressionType.Numeric);
            return null; //unreachable code
        }

        public virtual Func<TwitterStatus, string> GetStringValueProvider()
        {
            ThrowMismatchType(FilterExpressionType.String);
            return null; //unreachable code
        }

        public virtual Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            ThrowMismatchType(FilterExpressionType.Set);
            return null; //unreachable code
        }

        public virtual string GetBooleanSqlQuery()
        {
            ThrowMismatchType(FilterExpressionType.Boolean);
            return null; //unreachable code
        }

        public virtual string GetNumericSqlQuery()
        {
            ThrowMismatchType(FilterExpressionType.Numeric);
            return null; //unreachable code
        }

        public virtual string GetStringSqlQuery()
        {
            ThrowMismatchType(FilterExpressionType.String);
            return null; //unreachable code
        }

        public virtual string GetSetSqlQuery()
        {
            ThrowMismatchType(FilterExpressionType.Set);
            return null; //unreachable code
        }

        public abstract StringComparison GetStringComparison();

        protected void ThrowMismatchType(FilterExpressionType type)
        {
            throw FilterQueryException.CreateUnsupportedType(OperatorString, type, ToQuery());
        }

        protected void ThrowMismatchType(params FilterExpressionType[] types)
        {
            throw FilterQueryException.CreateUnsupportedType(OperatorString, types, ToQuery());
        }
    }

    public abstract class FilterSingleValueOperator : FilterOperatorBase
    {
        private FilterOperatorBase _value;

        public FilterOperatorBase Value
        {
            get { return _value; }
            set
            {
                if (_value != null)
                    _value.InvalidateRequested -= RaiseInvalidateFilter;
                _value = value;
                if (_value != null)
                    _value.InvalidateRequested += RaiseInvalidateFilter;
            }
        }

        public override void BeginLifecycle()
        {
            Value?.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            Value?.EndLifecycle();
        }

        public override StringComparison GetStringComparison()
        {
            return Value.GetStringComparison();
        }
    }

    public abstract class FilterTwoValueOperator : FilterOperatorBase
    {
        private FilterOperatorBase _leftValue;
        private FilterOperatorBase _rightValue;

        public FilterOperatorBase LeftValue
        {
            get { return _leftValue; }
            set
            {
                if (_leftValue != null)
                    _leftValue.InvalidateRequested -= RaiseInvalidateFilter;
                _leftValue = value;
                if (_leftValue != null)
                    _leftValue.InvalidateRequested += RaiseInvalidateFilter;
            }
        }

        public FilterOperatorBase RightValue
        {
            get { return _rightValue; }
            set
            {
                if (_rightValue != null)
                    _rightValue.InvalidateRequested -= RaiseInvalidateFilter;
                _rightValue = value;
                if (_rightValue != null)
                    _rightValue.InvalidateRequested += RaiseInvalidateFilter;
            }
        }

        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " " + OperatorString + " " + RightValue.ToQuery();
        }

        public override void BeginLifecycle()
        {
            LeftValue.BeginLifecycle();
            RightValue.BeginLifecycle();
        }

        public override void EndLifecycle()
        {
            LeftValue.EndLifecycle();
            RightValue.EndLifecycle();
        }

        public override StringComparison GetStringComparison()
        {
            return LeftValue.GetStringComparison() == StringComparison.CurrentCulture
                   || RightValue.GetStringComparison() == StringComparison.CurrentCulture
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;
        }
    }
    // ReSharper restore MemberCanBeProtected.Global
}