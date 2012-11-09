using System;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Filters.Sources
{
    /// <summary>
    /// General filter.
    /// </summary>
    public class FilterLocal : FilterSourceBase
    {
        public FilterLocal() { }

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => true;
        }

        public override string FilterKey
        {
            get { return "local"; }
        }

        public override string FilterValue
        {
            get { return null; }
        }
    }
}
