using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions
{
    public sealed class KQExpressionPair : KQExpressionBase
    {
        public bool IsNegated { get; set; }

        public bool IsAnd { get; set; }

        public KQExpressionBase LeftExpr { get; set; }

        public KQExpressionBase RightExpr { get; set; }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            if (IsAnd)
                return _ => LeftExpr.GetEvaluator()(_) != IsNegated && RightExpr.GetEvaluator()(_) != IsNegated;
            else
                return _ => LeftExpr.GetEvaluator()(_) != IsNegated || RightExpr.GetEvaluator()(_) != IsNegated;
        }

        public override string ToQuery()
        {
            return (IsNegated ? "!" : "") +
                "(" + LeftExpr.ToQuery() +
                (IsAnd ? " && " : " || ") +
                RightExpr.ToQuery() + ")";
        }
    }
}
