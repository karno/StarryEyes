using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Statuses
{
    public sealed class StatusText : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            if (status.RetweetedOriginal != null)
                return status.RetweetedOriginal.Text;
            else
                return status.Text;
        }

        public override string ToQuery()
        {
            return "text";
        }
    }

    public sealed class StatusSource : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.Source;
        }

        public override string ToQuery()
        {
            return "via"; // source, from is also ok
        }
    }

    public sealed class UserScreenName : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.ScreenName;
        }

        public override string ToQuery()
        {
            return "user.screen_name";
        }
    }

    public sealed class UserName : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Name;
        }

        public override string ToQuery()
        {
            return "user.name";
        }
    }

    public sealed class UserDescription : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Description;
        }

        public override string ToQuery()
        {
            return "user.description";
        }
    }

    public sealed class UserLocation : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Location;
        }

        public override string ToQuery()
        {
            return "user.location";
        }
    }

    public sealed class UserLanguage : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Language;
        }

        public override string ToQuery()
        {
            return "user.language";
        }
    }

    public sealed class RetweeterScreenName : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.ScreenName : null;
        }

        public override string ToQuery()
        {
            return "retweeter.screen_name";
        }
    }

    public sealed class RetweeterName : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Name : null;
        }

        public override string ToQuery()
        {
            return "retweeter.name";
        }
    }

    public sealed class RetweeterDescription : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Description : null;
        }

        public override string ToQuery()
        {
            return "retweeter.description";
        }
    }

    public sealed class RetweeterLocation : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Location : null;
        }

        public override string ToQuery()
        {
            return "retweeter.location";
        }
    }

    public sealed class RetweeterLanguage : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.String }; }
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Language : null;
        }

        public override string ToQuery()
        {
            return "retweeter.language";
        }
    }


}
