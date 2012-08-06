using System;
using System.Linq.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions
{
    public sealed class KQExpressionPair : KQExpressionBase
    {
        public bool IsNegated { get; set; }

        public bool IsAnd { get; set; }

        public KQExpressionBase LeftExpr { get; set; }

        public KQExpressionBase RightExpr { get; set; }

        public override Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression()
        {
            var left = LeftExpr.GetExpression();
            var right = RightExpr.GetExpression();
            Func<TwitterStatus, bool> lambda;
            bool isNegated = IsNegated;
            if (IsAnd)
                lambda = ts => left.Item1(ts) != IsNegated && right.Item1(ts) != IsNegated;
            else
                lambda = ts => left.Item1(ts) != IsNegated || right.Item1(ts) != IsNegated;
            Expression exprBody;
            var param = Expression.Parameter(typeof(Status), "status");
            if (IsAnd)
                exprBody = Expression.AndAlso(left.Item2.Body, right.Item2.Body);
            else
                exprBody = Expression.OrElse(left.Item2.Body, right.Item2.Body);

            var expr = Expression.Lambda<Func<Status, bool>>(exprBody, param);
            return new Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>>(lambda, expr);
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
