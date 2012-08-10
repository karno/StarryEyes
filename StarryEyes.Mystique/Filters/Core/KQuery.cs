using System.Linq;
using System;
using StarryEyes.SweetLady.DataModel;

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

        public bool Eval(TwitterStatus status)
        {
            throw new NotImplementedException();
        }
    }
}
