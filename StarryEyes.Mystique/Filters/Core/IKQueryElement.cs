using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public interface IKQueryElement
    {
        /// <summary>
        /// Get filter delegate
        /// </summary>
        Func<TwitterStatus, bool> GetEvaluator();

        /// <summary>
        /// Get query expression.
        /// </summary>
        string ToQuery();
    }
}
