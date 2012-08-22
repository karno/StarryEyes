using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public class KQOperatorNotEquals : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " != " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var supportedTypes = new[]
            {
                KQExpressionType.Boolean,
                KQExpressionType.Element,
                KQExpressionType.Numeric,
                KQExpressionType.String
            };
            var type = KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes, supportedTypes, RightValue.TransformableTypes);
            switch (type)
            {
                case KQExpressionType.Boolean:
                    return _ => LeftValue.GetBooleanValue(_) != RightValue.GetBooleanValue(_);
                case KQExpressionType.Numeric:
                    return _ => LeftValue.GetNumericValue(_) != RightValue.GetNumericValue(_);
                case KQExpressionType.String:
                    return _ => LeftValue.GetStringValue(_) != RightValue.GetStringValue(_);
                case KQExpressionType.Element:
                    return _ => LeftValue.GetElementValue(_) != RightValue.GetElementValue(_);
                default:
                    throw new KrileQueryException("Unsupported type on equals :" + type.ToString());
            }
        }
    }
}
