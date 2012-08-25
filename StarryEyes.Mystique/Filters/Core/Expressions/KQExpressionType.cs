using System;
using System.Collections.Generic;
using System.Linq;

namespace StarryEyes.Mystique.Filters.Core.Expressions
{
    public enum KQExpressionType
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

    public static class KQExpressionUtil
    {
        public static bool Assert(KQExpressionType type, params IEnumerable<KQExpressionType>[] checks)
        {
            return checks.All(_ => _.Contains(type));
        }

        public static KQExpressionType Fix(IEnumerable<KQExpressionType> candidates)
        {
            var types = new[] { KQExpressionType.Boolean, KQExpressionType.Numeric, KQExpressionType.String, KQExpressionType.Set };
            var ret = types.Intersect(candidates).First();
            if (ret == KQExpressionType.Invalid)
                throw new KrileQueryException("Can't fix expression type.");
            return ret;
        }

        public static KQExpressionType CheckDecide(
            KQExpressionType[] leftAvailable,
            KQExpressionType[] rightAvailable,
            KQExpressionType[] operatorAvailable)
        {
            var types = new[] { KQExpressionType.Boolean, KQExpressionType.Numeric, KQExpressionType.String, KQExpressionType.Set };
            var intersect = types.Intersect(leftAvailable).Intersect(operatorAvailable).Intersect(rightAvailable)
                .Concat(new[] { KQExpressionType.Invalid })
                .First();
            if (intersect == KQExpressionType.Invalid)
                throw new KrileQueryException("invalid types." + Environment.NewLine +
                    "left argument supports: " + leftAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine +
                    "right argument supports: " + rightAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine +
                    "operator supports: " + operatorAvailable.Select(t => t.ToString()).JoinString(", ") + Environment.NewLine);
            return intersect;
        }
    }
}
