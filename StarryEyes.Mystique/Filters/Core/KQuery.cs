using System;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core
{
    public sealed class KQuery : IKQueryElement
    {
        public KQSourceBase[] Sources;

        public KQExpressionBase PredicateTree;

        public string ToQuery()
        {
            return "from " + Sources.Select(s => s.ToQuery()).JoinString(", ") + " where " + PredicateTree.ToQuery();
        }

        public Func<TwitterStatus, bool> GetEvaluator()
        {
            return _ => Sources.Any(s => GetEvaluator()(_)) && PredicateTree.GetEvaluator()(_);
        }
    }
}
