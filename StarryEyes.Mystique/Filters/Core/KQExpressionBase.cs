using System;
using System.Linq.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public abstract class KQExpressionBase : IKQExpression
    {
        public abstract string ToQuery();

        public abstract Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression();
    }
}
