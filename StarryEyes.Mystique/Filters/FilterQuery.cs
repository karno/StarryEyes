using System;
using System.Linq;
using StarryEyes.Mystique.Filters.Core;
using StarryEyes.Mystique.Filters.Expressions;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters
{
    public sealed class FilterQuery : IFilterQueryElement
    {
        public KQSourceBase[] Sources;

        public FilterExpressionRoot PredicateTreeRoot;

        public string ToQuery()
        {
            return "from " + Sources.Select(s => s.ToQuery()).JoinString(", ") + " where " + PredicateTreeRoot.ToQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => Sources.Any(s => GetEvaluator()(_)) && PredicateTreeRoot.GetEvaluator()(_);
        }
    }
}
