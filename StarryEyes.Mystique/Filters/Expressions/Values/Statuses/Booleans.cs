using System.Linq;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;
using System.Collections.Generic;
using System;

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
            return "favorited";
        }
    }

    public sealed class UserIsProtected : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.User.IsProtected;
        }

        public override string ToQuery()
        {
            return "user.is_protected";
        }
    }

    public sealed class UserIsVerified : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.User.IsVerified;
        }

        public override string ToQuery()
        {
            return "user.is_verified";
        }
    }

    public sealed class UserIsTranslator : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.User.IsTranslator;
        }

        public override string ToQuery()
        {
            return "user.is_translator";
        }
    }

    public sealed class UserIsContributorsEnabled : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.User.IsContributorsEnabled;
        }

        public override string ToQuery()
        {
            return "user.is_contributors_enabled";
        }
    }

    public sealed class UserIsGeoEnabled : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.User.IsGeoEnabled;
        }

        public override string ToQuery()
        {
            return "user.is_geo_enabled";
        }
    }
}
