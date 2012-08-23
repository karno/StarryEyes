using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Immediates
{
    public class NumericValue : ValueBase
    {
        private long _value;

        public NumericValue(long value)
        {
            this._value = value;
        }

        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric, KQExpressionType.Element }; }
        }

        public override long GetNumericValue(TwitterStatus @unused)
        {
            return _value;
        }

        public override string ToQuery()
        {
            return _value.ToString();
        }
    }
}
