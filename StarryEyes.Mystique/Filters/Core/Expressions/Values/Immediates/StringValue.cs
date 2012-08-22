using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Immediates
{
    public class StringValue : ValueBase
    {
        private string _value;

        public StringValue(string value)
        {
            this._value = value;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get
            {
                return new[] { KQExpressionType.String };
            }
        }

        public override string GetStringValue(TwitterStatus @unused)
        {
            return _value;
        }

        public override string ToQuery()
        {
            return "\"" + _value + "\"";
        }
    }
}
