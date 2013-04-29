using System;
using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Expressions.Operators
{
    public abstract class FilterOperatorBase : FilterExpressionBase
    {
        public abstract IEnumerable<FilterExpressionType> SupportedTypes { get; }

        public virtual Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            throw new FilterQueryException("Unsupported transforms to boolean.", ToQuery());
        }

        public virtual Func<TwitterStatus, long> GetNumericValueProvider()
        {
            throw new FilterQueryException("Unsupported transforms to numeric.", ToQuery());
        }

        public virtual Func<TwitterStatus, string> GetStringValueProvider()
        {
            throw new FilterQueryException("Unsupported transforms to string.", ToQuery());
        }

        public virtual Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            throw new FilterQueryException("Unsupported transforms to set.", ToQuery());
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
                    _value.ReapplyRequested -= this.RaiseReapplyFilter;
                _value = value;
                if (_value != null)
                    _value.ReapplyRequested += this.RaiseReapplyFilter;
            }
        }

        public override void BeginLifecycle()
        {
            if (Value != null)
            {
                Value.BeginLifecycle();
            }
        }

        public override void EndLifecycle()
        {
            if (Value != null)
            {
                Value.EndLifecycle();
            }
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
                    _leftValue.ReapplyRequested -= this.RaiseReapplyFilter;
                _leftValue = value;
                if (_leftValue != null)
                    _leftValue.ReapplyRequested += this.RaiseReapplyFilter;
            }
        }

        public FilterOperatorBase RightValue
        {
            get { return _rightValue; }
            set
            {
                if (_rightValue != null)
                    _rightValue.ReapplyRequested -= this.RaiseReapplyFilter;
                _rightValue = value;
                if (_rightValue != null)
                    _rightValue.ReapplyRequested += this.RaiseReapplyFilter;
            }
        }

        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " " + OperatorString + " " + RightValue.ToQuery();
        }

        protected abstract string OperatorString { get; }

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
    }
}
