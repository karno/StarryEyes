using System;
using System.Collections.Generic;
using Cadena.Data;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Filters.Parsing;

namespace StarryEyes.Filters.Expressions.Values.Immediates
{
    public class StringValue : ValueBase
    {
        private readonly string _value;

        public StringValue(string value)
        {
            _value = value;
        }

        protected override string OperatorString => Value.Quote();

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public string Value => _value;

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => Value;
        }

        public override string GetStringSqlQuery()
        {
            return Value.Escape().Wrap();
        }

        public override string ToQuery()
        {
            return Value.EscapeForQuery().Quote();
        }
    }
}