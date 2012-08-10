using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public abstract class KQExpressionBase : IKQExpression
    {
        public abstract string ToQuery();

        public abstract bool Eval(TwitterStatus status);
    }
}
