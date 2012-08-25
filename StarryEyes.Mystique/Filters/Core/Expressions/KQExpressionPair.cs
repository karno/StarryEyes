using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions
{
    public sealed class KQExpressionPair : KQExpressionBase
    {
        public bool IsNegated { get; set; }

        public bool IsAnd { get; set; }

        public IEnumerable<KQExpressionBase> Expressions { get; set; }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            var evals = Expressions.Select(expr => expr.GetEvaluator());
            if (IsAnd)
                return _ => evals.All(e => e(_) != IsNegated);
            else
                return _ => evals.Any(e => e(_) != IsNegated);
        }

        public override string ToQuery()
        {
            return (IsNegated ? "!" : "") + "(" +
                Expressions.Select(_ => _.ToQuery()).JoinString(IsAnd ? " && " : " || ")
                + ")";
        }
    }
}
