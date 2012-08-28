using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Sources
{
    /// <summary>
    /// General filter.
    /// </summary>
    public class FilterGeneral : FilterSourceBase
    {
        public FilterGeneral() { }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => true;
        }

        public override string FilterKey
        {
            get { return "general"; }
        }

        public override string FilterValue
        {
            get { return null; }
        }
    }
}
