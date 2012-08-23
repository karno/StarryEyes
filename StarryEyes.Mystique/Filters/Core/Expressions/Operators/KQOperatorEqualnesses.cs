using System;
using System.Text.RegularExpressions;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Mystique.Filters.Core.Expressions.Values.Immediates;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public class KQOperatorEquals : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery() + " == " + RightValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var supportedTypes = new[]
            {
                KQExpressionType.Boolean,
                KQExpressionType.Numeric,
                KQExpressionType.String
            };
            var type = KQExpressionUtil.CheckDecide(
                LeftValue.TransformableTypes, supportedTypes, RightValue.TransformableTypes);
            switch (type)
            {
                case KQExpressionType.Boolean:
                    return _ => LeftValue.GetBooleanValue(_) == RightValue.GetBooleanValue(_);
                case KQExpressionType.Numeric:
                    return _ => LeftValue.GetNumericValue(_) == RightValue.GetNumericValue(_);
                case KQExpressionType.String:
                    var side = StringArgumentSide.None;
                    // determine side of argument
                    if (LeftValue is StringValue)
                        side = StringArgumentSide.Left;
                    else if (RightValue is StringValue)
                        side = StringArgumentSide.Right;
                    return _ => StringMatch(LeftValue.GetStringValue(_), RightValue.GetStringValue(_), side);
                default:
                    throw new KrileQueryException("Unsupported type on equals :" + type.ToString());
            }
        }

        public static bool StringMatch(string left, string right, StringArgumentSide side)
        {
            if (side == StringArgumentSide.None)
                return left == right;
            if (side == StringArgumentSide.Left)
            {
                // set right as argument.
                var cons = right;
                right = left;
                left = cons;
            }
            if (right.StartsWith("/"))
            {
                // regex
                try
                {
                    return Regex.IsMatch(left, right.Substring(1));
                }
                catch // format error?
                {
                    return false;
                }
            }
            else
            {
                var startsWith = right.StartsWith("^");
                var endsWith = right.EndsWith("$");
                if (startsWith && endsWith)
                {
                    return left.Equals(right.Substring(1, right.Length - 2),
                        StringComparison.CurrentCultureIgnoreCase);
                }
                else if (startsWith)
                {
                    return left.StartsWith(right.Substring(1),
                        StringComparison.CurrentCultureIgnoreCase);
                }
                else if (endsWith)
                {
                    return left.EndsWith(right.Substring(right.Length - 1),
                        StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    return left.IndexOf(right, StringComparison.CurrentCultureIgnoreCase) >= 0;
                }
            }
        }

        public enum StringArgumentSide
        {
            Left,
            Right,
            None,
        }
    }

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
                default:
                    throw new KrileQueryException("Unsupported type on equals :" + type.ToString());
            }
        }
    }
}
