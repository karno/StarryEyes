using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters
{
    public interface IFilterQueryElement
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
