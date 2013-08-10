using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Users
{
    public sealed class UserIsProtected : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.GetOriginal().User.IsProtected;
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
            return _ => _.GetOriginal().User.IsVerified;
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
            return _ => _.GetOriginal().User.IsTranslator;
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
            return _ => _.GetOriginal().User.IsContributorsEnabled;
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
            return _ => _.GetOriginal().User.IsGeoEnabled;
        }

        public override string ToQuery()
        {
            return "user.is_geo_enabled";
        }
    }

    public sealed class RetweeterIsProtected : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null && _.User.IsProtected;
        }

        public override string ToQuery()
        {
            return "retweeter.is_protected";
        }
    }

    public sealed class RetweeterIsVerified : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null && _.User.IsVerified;
        }

        public override string ToQuery()
        {
            return "retweeter.is_verified";
        }
    }

    public sealed class RetweeterIsTranslator : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null && _.User.IsTranslator;
        }

        public override string ToQuery()
        {
            return "retweeter.is_translator";
        }
    }

    public sealed class RetweeterIsContributorsEnabled : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null && _.User.IsContributorsEnabled;
        }

        public override string ToQuery()
        {
            return "retweeter.is_contributors_enabled";
        }
    }

    public sealed class RetweeterIsGeoEnabled : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null && _.User.IsGeoEnabled;
        }

        public override string ToQuery()
        {
            return "retweeter.is_geo_enabled";
        }
    }
}
