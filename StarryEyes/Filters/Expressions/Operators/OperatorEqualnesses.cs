using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions.Values.Immediates;

namespace StarryEyes.Filters.Expressions.Operators
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
                FilterExpressionType.String,
                FilterExpressionType.Set
            };
            var intersect = LeftValue.SupportedTypes.Intersect(RightValue.SupportedTypes).ToArray();
            if (!intersect.Any())
                throw new FilterQueryException(
                    "Value type is mismatched. Can't compare each other." + Environment.NewLine +
                    "Left argument is: " + LeftValue.SupportedTypes
                    .Select(t => t.ToString()).JoinString(", ") + Environment.NewLine +
                    "Right argument is: " + RightValue.SupportedTypes
                    .Select(t => t.ToString()).JoinString(", "),
                    this.ToQuery());
            var type = supportedTypes.Intersect(intersect).First();
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
                    throw new FilterQueryException("Unsupported type on equals :" + type.ToString(),
                        this.ToQuery());
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
            var startsWith = right.StartsWith("^");
            var endsWith = right.EndsWith("$");
            if (startsWith && endsWith)
            {
                return left.Equals(right.Substring(1, right.Length - 2),
                                   StringComparison.CurrentCultureIgnoreCase);
            }
            if (startsWith)
            {
                return left.StartsWith(right.Substring(1),
                                       StringComparison.CurrentCultureIgnoreCase);
            }
            if (endsWith)
            {
                return left.EndsWith(right.Substring(right.Length - 1),
                                     StringComparison.CurrentCultureIgnoreCase);
            }
            return left.IndexOf(right, StringComparison.CurrentCultureIgnoreCase) >= 0;
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
