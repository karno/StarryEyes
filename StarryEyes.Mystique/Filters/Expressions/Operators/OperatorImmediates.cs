using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Operators
{
    public class FilterNegate : FilterSingleValueOperator
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            if (!FilterExpressionUtil.Assert(FilterExpressionType.Boolean, Value.SupportedTypes))
                throw new FilterQueryException("Negate operator must be have boolean value.", ToQuery());
            return _ => !Value.GetBooleanValueProvider()(_);
        }

        public override string ToQuery()
        {
            return "!" + Value.ToQuery();
        }
    }

    /// <summary>
    /// Pseudo filter.
    /// </summary>
    public class FilterBracket : FilterSingleValueOperator
    {
        private FilterOperatorBase _inner;
        public FilterBracket(FilterOperatorBase inner)
        {
            this._inner = inner;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { return _inner.SupportedTypes; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _inner.GetBooleanValueProvider();
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _inner.GetNumericValueProvider();
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            return _inner.GetSetValueProvider();
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _inner.GetStringValueProvider();
        }

        public override string ToQuery()
        {
            return "(" + _inner.ToQuery() + ")";
        }
    }
}
