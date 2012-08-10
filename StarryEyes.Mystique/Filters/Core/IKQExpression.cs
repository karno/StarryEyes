using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public interface IKQExpression
    {
        /// <summary>
        /// Get filter delegate and expression tree
        /// </summary>
        bool Eval(TwitterStatus status);

        /// <summary>
        /// Get query expression.
        /// </summary>
        string ToQuery();
    }
}
