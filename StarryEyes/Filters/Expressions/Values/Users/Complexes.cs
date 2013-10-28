using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Users
{
    public sealed class User : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.String;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().User.Id;
        }

        public override string GetNumericSqlQuery()
        {
            return "BaseUserId";
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
            return "user";
        }
    }

    public sealed class Retweeter : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.String;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.Id : -1;
        }

        public override string GetNumericSqlQuery()
        {
            return Coalesce("RetweeterId", -1);
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.ScreenName : String.Empty;
        }

        public override string GetStringSqlQuery()
        {
            return Coalesce("(select ScreenName from User where Id = status.RetweeterId limit 1)", "");
        }

        public override string ToQuery()
        {
            return "retweeter";
        }
    }
}
