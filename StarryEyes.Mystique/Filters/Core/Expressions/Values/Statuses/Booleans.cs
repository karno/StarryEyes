using System.Linq;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Statuses
{
    public sealed class StatusIsDirectMessage : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.StatusType == StatusType.DirectMessage;
        }

        public override string ToQuery()
        {
            return "is_dm"; // is_direct_message / is_message are also ok
        }
    }

    public sealed class StatusIsFavorited : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return AccountDataStore.GetAccountDatas().Any(_ => status.FavoritedUsers.Contains(_.AccountId));
        }

        public override string ToQuery()
        {
            return "favorited";
        }
    }

    public sealed class UserIsProtected : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.User.IsProtected;
        }

        public override string ToQuery()
        {
            return "user.is_protected";
        }
    }

    public sealed class UserIsVerified : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.User.IsVerified;
        }

        public override string ToQuery()
        {
            return "user.is_verified";
        }
    }

    public sealed class UserIsTranslator : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.User.IsTranslator;
        }

        public override string ToQuery()
        {
            return "user.is_translator";
        }
    }

    public sealed class UserIsContributorsEnabled : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.User.IsContributorsEnabled;
        }

        public override string ToQuery()
        {
            return "user.is_contributors_enabled";
        }
    }

    public sealed class UserIsGeoEnabled : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Boolean }; }
        }

        public override bool GetBooleanValue(TwitterStatus status)
        {
            return status.User.IsGeoEnabled;
        }

        public override string ToQuery()
        {
            return "user.is_geo_enabled";
        }
    }
}
