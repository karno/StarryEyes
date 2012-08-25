using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarryEyes.Mystique.Filters.Expressions.Values.Immediates;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Operators
{
    public class FilterOperatorEquals : FilterTwoValueOperator
    {
        protected override string OperatorString
        {
            get { return "=="; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var supportedTypes = new[]
            {
                FilterExpressionType.Boolean,
                FilterExpressionType.Numeric,
                FilterExpressionType.String
            };
            var type = FilterExpressionUtil.CheckDecide(
                LeftValue.SupportedTypes, supportedTypes, RightValue.SupportedTypes);
            switch (type)
            {
                case FilterExpressionType.Boolean:
                    var lbp = LeftValue.GetBooleanValueProvider();
                    var rbp = RightValue.GetBooleanValueProvider();
                    return _ => lbp(_) == rbp(_);
                case FilterExpressionType.Numeric:
                    var lnp = LeftValue.GetNumericValueProvider();
                    var rnp = RightValue.GetNumericValueProvider();
                    return _ => lnp(_) == rnp(_);
                case FilterExpressionType.String:
                    var side = StringArgumentSide.None;
                    // determine side of argument
                    if (LeftValue is StringValue)
                        side = StringArgumentSide.Left;
                    else if (RightValue is StringValue)
                        side = StringArgumentSide.Right;
                    var lsp = LeftValue.GetStringValueProvider();
                    var rsp = RightValue.GetStringValueProvider();
                    return _ => StringMatch(lsp(_), rsp(_), side);
                default:
                    throw new FilterQueryException("Unsupported type on equals :" + type.ToString());
            }
        }

        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public static bool StringMatch(string left, string right, StringArgumentSide side)
        {
            if (left == null || right == null) // null value is not accepted.
                return false;
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

    public class FilterOperatorNotEquals : FilterOperatorEquals
    {
        protected override string OperatorString
        {
            get
            {
                return "!=";
            }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => !base.GetBooleanValueProvider()(_);
        }
    }
}
