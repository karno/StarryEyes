using System;
using System.Linq.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public interface IKQExpression
    {
        /// <summary>
        /// Get filter delegate and expression tree
        /// </summary>
        Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression();

        /// <summary>
        /// Get query expression.
        /// </summary>
        string ToQuery();
    }
}
