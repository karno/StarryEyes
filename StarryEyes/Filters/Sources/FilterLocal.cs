using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions;

namespace StarryEyes.Filters.Sources
{
    /// <summary>
    /// General filter.
    /// </summary>
    public class FilterLocal : FilterSourceBase
    {
        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return FilterExpressionBase.Tautology;
        }

        public override string GetSqlQuery()
        {
            return "1";
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
