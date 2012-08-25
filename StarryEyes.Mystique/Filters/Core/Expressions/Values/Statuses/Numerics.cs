using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Statuses
{
    public sealed class StatusId : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().Id;
        }

        public override string ToQuery()
        {
            return "id";
        }
    }

    public sealed class UserId : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Id;
        }

        public override string ToQuery()
        {
            return "user.id";
        }
    }

    public sealed class UserStatuses : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.StatusesCount;
        }

        public override string ToQuery()
        {
            return "user.statuses"; // user.statuses_count is also ok
        }
    }

    public sealed class UserFriends : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.FriendsCount;
        }

        public override string ToQuery()
        {
            return "user.followings"; // user.followings_count, user_friends, user.friends_count is also ok
        }
    }

    public sealed class UserFollowers : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.FollowersCount;
        }

        public override string ToQuery()
        {
            return "user.followers";
        }
    }

    public sealed class UserFavroites : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.FavoritesCount;
        }

        public override string ToQuery()
        {
            return "user.favorites";
        }
    }

    public sealed class UserListed : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.ListedCount;
        }

        public override string ToQuery()
        {
            return "user.listed";
        }
    }

    public sealed class RetweeterId : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Id : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.id";
        }
    }

    public sealed class RetweeterStatuses : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.StatusesCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.statuses"; // user.statuses_count is also ok
        }
    }

    public sealed class RetweeterFriends : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.FriendsCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.followings"; // user.followings_count, user_friends, user.friends_count is also ok
        }
    }

    public sealed class RetweeterFollowers : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.FollowersCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.followers";
        }
    }

    public sealed class RetweeterFavroites : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.FavoritesCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.favorites";
        }
    }

    public sealed class RetweeterListed : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.ListedCount : -1;
        }

        public override string ToQuery()
        {
            return "retweeter.listed";
        }
    }


}
