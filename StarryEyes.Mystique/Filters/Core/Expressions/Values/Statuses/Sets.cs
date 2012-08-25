using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Statuses
{
    public sealed class StatusFavoriteds : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus status)
        {
            return status.FavoritedUsers ?? new long[0];
        }

        public override string ToQuery()
        {
            return "favorited";
        }
    }

    public sealed class StatusRetweeteds : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Set }; }
        }

        public override ICollection<long> GetSetValue(TwitterStatus status)
        {
            return status.RetweetedUsers ?? new long[0];
        }

        public override string ToQuery()
        {
            return "retweeted";
        }
    }
}
