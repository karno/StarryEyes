using System;
using System.Collections.Generic;
using System.Globalization;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Immediates
{
    public class NumericValue : ValueBase
    {
        private readonly long _value;

        public NumericValue(long value)
        {
            this._value = value;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _value;
        }

        public override string ToQuery()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
