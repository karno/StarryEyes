using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Immediates
{
    public class NumericValue : ValueBase
    {
        private long _value;

        public NumericValue(long value)
        {
            this._value = value;
        }

        public override IEnumerable<FilterExpressionType>  SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _value;
        }

        public override string ToQuery()
        {
            return _value.ToString();
        }
    }
}
