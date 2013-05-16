using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Filters.Expressions.Values.Statuses
{
    public sealed class StatusIsDirectMessage : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.StatusType == StatusType.DirectMessage;
        }

        public override string ToQuery()
        {
            return "direct_message";
        }
    }

    public sealed class StatusIsFavorited : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var accounts = AccountRelationDataStore.AccountRelations.Select(a => a.AccountId).ToArray();
            return s => s.FavoritedUsers != null && accounts.Any(a => s.FavoritedUsers.Contains(a));
        }

        public override string ToQuery()
        {
            return "favorited";
        }
    }

    public sealed class StatusIsRetweeted : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            var accounts = AccountRelationDataStore.AccountRelations.Select(a => a.AccountId).ToArray();
            return s => s.RetweetedUsers != null && accounts.Any(a => s.RetweetedUsers.Contains(a));
        }

        public override string ToQuery()
        {
            return "retweeted";
        }
    }

    public sealed class StatusIsRetweet : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null;
        }

        public override string ToQuery()
        {
            return "retweet";
        }
    }

}
