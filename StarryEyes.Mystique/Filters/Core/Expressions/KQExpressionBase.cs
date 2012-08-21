using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public abstract class KQExpressionBase : IKQueryElement
    {
        public abstract string ToQuery();

        public abstract Func<TwitterStatus, bool> GetEvaluator();
    }
}
