using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Expressions.Operators;

namespace StarryEyes.Filters
{
    public static class FilterSystemUtil
    {
        public static IEnumerable<long> InReplyToUsers(TwitterStatus status)
        {
            if (status.StatusType == StatusType.DirectMessage)
            {
                return new[] { status.Recipient.Id };
            }
            if (status.Entities == null)
            {
                return Enumerable.Empty<long>();
            }
            return status.Entities
                         .Where(e => e.EntityType == EntityType.UserMentions)
                         .Select(e => e.UserId ?? 0)
                         .Where(id => id != 0);
        }

        public static TwitterStatus GetOriginal(this TwitterStatus status)
        {
            return status.RetweetedOriginal ?? status;
        }

        public static FilterOperatorBase And(this FilterOperatorBase left, FilterOperatorBase right)
        {
            if (left is FilterOperatorOr)
            {
                left = new FilterBracket { Value = left };
            }
            if (right is FilterOperatorOr)
            {
                right = new FilterBracket { Value = right };
            }
            return new FilterOperatorAnd
            {
                LeftValue = left,
                RightValue = right
            };
        }

        public static FilterOperatorBase Or(this FilterOperatorBase left, FilterOperatorBase right)
        {
            return new FilterOperatorOr
            {
                LeftValue = left,
                RightValue = right
            };
        }
    }
}
