using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Users
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

        public override string GetNumericSqlQuery()
        {
            return "BaseUserId";
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

        public override string GetNumericSqlQuery()
        {
            return "(select StatusesCount from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.statuses"; // user.status_count is also ok
        }
    }

    public sealed class UserFollowing : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.FollowingsCount;
        }

        public override string GetNumericSqlQuery()
        {
            return "(select FollowingsCount from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.following"; // user.followings_count, user.friends, user.friends_count is also ok
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

        public override string GetNumericSqlQuery()
        {
            return "(select FollowersCount from User where Id = status.BaseUserId limit 1)";
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

        public override string GetNumericSqlQuery()
        {
            return "(select FavoritesCount from User where Id = status.BaseUserId limit 1)";
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

        public override string GetNumericSqlQuery()
        {
            return "(select ListedCount from User where Id = status.BaseUserId limit 1)";
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

        public override string GetNumericSqlQuery()
        {
            return "RetweetOriginalId";
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

        public override string GetNumericSqlQuery()
        {
            return "(select StatusesCount from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.statuses"; // retweeter.status_count is also ok
        }
    }

    public sealed class RetweeterFollowing : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.FollowingsCount : -1;
        }

        public override string GetNumericSqlQuery()
        {
            return "(select FollowingsCount from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.following"; // user.followings_count, user_friends, user.friends_count is also ok
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

        public override string GetNumericSqlQuery()
        {
            return "(select FollowersCount from User where Id = status.RetweeterId limit 1)";
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

        public override string GetNumericSqlQuery()
        {
            return "(select FavoritesCount from User where Id = status.RetweeterId limit 1)";
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

        public override string GetNumericSqlQuery()
        {
            return "(select ListedCount from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.listed";
        }
    }
}
