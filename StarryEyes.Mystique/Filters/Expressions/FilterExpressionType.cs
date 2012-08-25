using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Mystique.Filters.Expressions
{
    public enum FilterExpressionType
    {
        /// <summary>
        /// Invalid type. please do not use this.
        /// </summary>
        Invalid,
        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean,
        /// <summary>
        /// Numeric value
        /// </summary>
        Numeric,
        /// <summary>
        /// Text string
        /// </summary>
        String,
        /// <summary>
        /// Numerical set (has 0~N Elements)
        /// </summary>
        Set,
    }

    public static class FilterExpressionUtil
    {
        public static bool Assert(FilterExpressionType type, params IEnumerable<FilterExpressionType>[] checks)
        {
            return checks.All(_ => _.Contains(type));
        }

        public static FilterExpressionType Fix(IEnumerable<FilterExpressionType> candidates)
        {
            var types = new[] { FilterExpressionType.Boolean, FilterExpressionType.Numeric, FilterExpressionType.String, FilterExpressionType.Set };
            var ret = types.Intersect(candidates).First();
            if (ret == FilterExpressionType.Invalid)
                throw new FilterQueryException("Can't fix expression type.");
            return ret;
        }

        public static FilterExpressionType CheckDecide(
            IEnumerable<FilterExpressionType> leftAvailable,
            IEnumerable<FilterExpressionType> rightAvailable,
            IEnumerable<FilterExpressionType> operatorAvailable)
        {
            var types = new[] { FilterExpressionType.Boolean, FilterExpressionType.Numeric, FilterExpressionType.String, FilterExpressionType.Set };
            var intersect = types.Intersect(leftAvailable).Intersect(operatorAvailable).Intersect(rightAvailable)
                .Concat(new[] { FilterExpressionType.Invalid })
                .First();
            if (intersect == FilterExpressionType.Invalid)
                throw new FilterQueryException("invalid types." + Environment.NewLine +
                    "left argument supports: " + leftAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine +
                    "right argument supports: " + rightAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine +
                    "operator supports: " + operatorAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine);
            return intersect;
        }
    }
}
