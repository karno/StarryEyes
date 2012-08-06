using System.Linq;

namespace StarryEyes.Mystique.Filters.Core
{
    public sealed class KQuery : IKQExpression
    {
        public KQSourceBase[] Sources;

        public KQExpressionBase PredicateTree;

        public string ToQuery()
        {
            return "from " + Sources.Select(s => s.ToQuery()).JoinString(", ") + " where " + PredicateTree.ToQuery();
        }

        public System.Tuple<System.Func<SweetLady.DataModel.TwitterStatus, bool>, System.Linq.Expressions.Expression<System.Func<Status, bool>>> GetExpression()
        {
            throw new System.NotImplementedException();
        }
    }
}
