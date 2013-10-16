using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters.Expressions.Values.Immediates
{
    public class StringValue : ValueBase
    {
        private readonly string _value;

        public StringValue(string value)
        {
            this._value = value;
        }

        protected override string OperatorString
        {
            get { return "\"" + _value + "\""; }
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

        public override string GetStringSqlQuery()
        {
            return _value.Escape().Wrap();
        }

        public override string ToQuery()
        {
            return "\"" + _value + "\"";
        }
    }
}
