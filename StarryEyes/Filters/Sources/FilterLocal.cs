using System;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters.Expressions;

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
            return FilterExpressionBase.Tautology;
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
