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
            get { return "\"" + this.Value + "\""; }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.String;
            }
        }

        public string Value
        {
            get { return this._value; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => this.Value;
        }

        public override string GetStringSqlQuery()
        {
            return this.Value.Escape().Wrap();
        }

        public override string ToQuery()
        {
            return "\"" + this.Value.Replace("\"", "\\\"") + "\"";
        }
    }
}
