using System;
using System.Linq.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    /// <summary>
    /// Tweets source of status
    /// </summary>
    public abstract class KQSourceBase : IKQExpression
    {
        public abstract string ToQuery();

        public abstract Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression();
    }
}
