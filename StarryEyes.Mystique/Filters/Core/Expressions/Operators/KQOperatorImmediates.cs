using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    /// <summary>
    /// Immediate value for booleans.
    /// </summary>
    public class KQOperatorImmediate : KQOperatorBase
    {
        public override string ToQuery()
        {
            return LeftValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            if (!KQExpressionUtil.Assert(KQExpressionType.Boolean, LeftValue.TransformableTypes))
                throw new KrileQueryException("Immediate operator must have boolean value.");
            return _ => LeftValue.GetBooleanValue(_);
        }
    }

    public class KQOperatorImmediateNegate : KQOperatorBase
    {
        public override string ToQuery()
        {
            return "!" + LeftValue.ToQuery();
        }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            if (!KQExpressionUtil.Assert(KQExpressionType.Boolean, LeftValue.TransformableTypes))
                throw new KrileQueryException("Negate operator must have boolean value.");
            return _ => !LeftValue.GetBooleanValue(_);
        }
    }

}
