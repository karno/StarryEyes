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

        public  bool Eval(TwitterStatus status)
        {
            var type = KQExpressionUtil.CheckDecide(LeftValue.TransformableTypes, RightValue.TransformableTypes, this.TransformableTypes);
            return EvalInternal(status, type);
        }

        protected abstract bool EvalInternal(TwitterStatus status, KQExpressionType type);
    }
}
