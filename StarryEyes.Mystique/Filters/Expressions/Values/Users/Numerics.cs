using System;
using System.Collections.Generic;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Users
{
    public sealed class UserId : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.Id;
        }

        public override string ToQuery()
        {
            return "user.id";
        }
    }

    public sealed class UserStatuses : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.StatusesCount;
        }

        public override string ToQuery()
        {
            return "user.statuses"; // user.status_count is also ok
        }
    }

    public sealed class UserFriends : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.FriendsCount;
        }

        public override string ToQuery()
        {
            return "user.followings"; // user.followings_count, user.friends, user.friends_count is also ok
        }
    }

    public sealed class UserFollowers : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.FollowersCount;
        }

        public override string ToQuery()
        {
            return "user.followers";
        }
    }

    public sealed class UserFavroites : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.FavoritesCount;
        }

        public override string ToQuery()
        {
            return "user.favorites";
        }
    }

    public sealed class UserListed : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.ListedCount;
        }

        public override string ToQuery()
        {
            return "user.listed";
        }
    }

    public sealed class RetweeterId : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Id : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.id";
        }
    }

    public sealed class RetweeterStatuses : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.StatusesCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.statuses"; // retweeter.status_count is also ok
        }
    }

    public sealed class RetweeterFriends : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.FriendsCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.followings"; // user.followings_count, user_friends, user.friends_count is also ok
        }
    }

    public sealed class RetweeterFollowers : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.FollowersCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.followers";
        }
    }

    public sealed class RetweeterFavroites : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.FavoritesCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.favorites";
        }
    }

    public sealed class RetweeterListed : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.ListedCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.listed";
        }
    }
}
