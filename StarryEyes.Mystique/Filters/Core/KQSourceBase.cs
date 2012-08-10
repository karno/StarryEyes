using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class KQSourceBase : IKQExpression
    {
        public abstract string ToQuery();

        public abstract bool Eval(TwitterStatus status);
    }
}
