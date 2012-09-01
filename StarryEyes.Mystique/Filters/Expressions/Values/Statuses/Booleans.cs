using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Statuses
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
            return "is_dm"; // is_direct_message / is_message are also ok
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
            return _ => AccountDataStore.GetAccountDatas().Any(ad => _.FavoritedUsers.Contains(ad.AccountId));
        }

        public override string ToQuery()
        {
            return "is_favorited";
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
            return _ => AccountDataStore.GetAccountDatas().Any(ad => _.RetweetedUsers.Contains(ad.AccountId));
        }

        public override string ToQuery()
        {
            return "is_retweeted";
        }
    }
}
