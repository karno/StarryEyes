using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Users
{
    public sealed class UserScreenName : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.ScreenName;
        }

        public override string GetStringSqlQuery()
        {
            return "(select ScreenName from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.screen_name";
        }
    }

    public sealed class UserName : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.Name;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Name from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.name";
        }
    }

    public sealed class UserDescription : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.Description;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Description from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.description";
        }
    }

    public sealed class UserLocation : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.Location;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Location from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.location";
        }
    }

    public sealed class UserLanguage : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.Language;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Language from User where Id = status.BaseUserId limit 1)";
        }

        public override string ToQuery()
        {
            return "user.language";
        }
    }

    public sealed class RetweeterScreenName : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.ScreenName : null;
        }

        public override string GetStringSqlQuery()
        {
            return "(select ScreenName from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.screen_name";
        }
    }

    public sealed class RetweeterName : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Name : null;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Name from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.name";
        }
    }

    public sealed class RetweeterDescription : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Description : null;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Description from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.description";
        }
    }

    public sealed class RetweeterLocation : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Location : null;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Location from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.location";
        }
    }

    public sealed class RetweeterLanguage : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.String; }
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Language : null;
        }

        public override string GetStringSqlQuery()
        {
            return "(select Language from User where Id = status.RetweeterId limit 1)";
        }

        public override string ToQuery()
        {
            return "retweeter.language";
        }
    }
}
