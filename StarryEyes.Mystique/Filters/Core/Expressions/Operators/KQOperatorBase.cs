using System;
using System.Linq.Expressions;
using StarryEyes.Mystique.Filters.Core.Expressions.Values;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public abstract class KQOperatorBase : IKQExpression
    {
        public ValueBase LeftValue { get; set; }

        public ValueBase RightValue { get; set; }

        public abstract KQExpressionType[] TransformableTypes { get; }

        public abstract string ToQuery();

        public abstract Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression();

        protected abstract Func<TwitterStatus, bool> GenerateLambda(KQExpressionType type);

        protected abstract Expression<Func<Status, bool>> GenerateExpression(KQExpressionType type);
    }
}
