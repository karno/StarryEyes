using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Immediates
{
    public class StringValue : ValueBase
    {
        private string _value;

        public StringValue(string value)
        {
            this._value = value;
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.String;
            }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _value;
        }

        public override string ToQuery()
        {
            return "\"" + _value + "\"";
        }
    }
}
