using System;
using System.Collections.Generic;
using System.Globalization;
using Cadena.Data;

namespace StarryEyes.Filters.Expressions.Values.Immediates
{
    public class NumericValue : ValueBase
    {
        private readonly long _value;

        public NumericValue(long value)
        {
            _value = value;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _value;
        }

        public override string GetNumericSqlQuery()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public override string ToQuery()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}