using System.Linq;

namespace StarryEyes.Mystique.Filters.Core
{
    public sealed class Query
    {
        public SourceBase[] Sources;

        public PredicateBase PredicateTree;

        public string ToQuery()
        {
            return "from " + Sources.Select(s => s.ToQuery()).JoinString(", ") + " where " + PredicateTree.ToQuery();
        }
    }
}
