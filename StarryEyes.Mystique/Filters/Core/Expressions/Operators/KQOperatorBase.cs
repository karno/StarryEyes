using System;
using System.Linq.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Operators
{
    public abstract class KQOperatorBase : IKQExpression
    {
        public abstract string ToQuery();

        public abstract Tuple<Func<TwitterStatus, bool>, Expression<Func<Status, bool>>> GetExpression();
    }

    public enum Types
    {
        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean,
        /// <summary>
        /// Numeric value
        /// </summary>
        Numeric,
        /// <summary>
        /// Text string
        /// </summary>
        String,
        /// <summary>
        /// Element of a set
        /// </summary>
        Element,
        /// <summary>
        /// Set (had 0~N Elements)
        /// </summary>
        Set,
    }
}
