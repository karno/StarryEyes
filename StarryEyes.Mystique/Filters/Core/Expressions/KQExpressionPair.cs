using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions
{
    public sealed class KQExpressionPair : KQExpressionBase
    {
        public bool IsNegated { get; set; }

        public bool IsAnd { get; set; }

        public KQExpressionBase LeftExpr { get; set; }

        public KQExpressionBase RightExpr { get; set; }

        public override bool Eval(TwitterStatus status)
        {
            bool isNegated = IsNegated;
            if (IsAnd)
                return LeftExpr.Eval(status) != IsNegated && RightExpr.Eval(status) != IsNegated;
            else
                return LeftExpr.Eval(status) != IsNegated || RightExpr.Eval(status) != IsNegated;
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
