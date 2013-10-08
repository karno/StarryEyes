using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Filters.Parsing;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Expressions.Values.Statuses
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

        public override string GetBooleanSqlQuery()
        {
            return "StatusType = " + ((int)StatusType.DirectMessage).ToString(CultureInfo.InvariantCulture);
        }

        public override string ToQuery()
        {
            return "direct_message";
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
            var accounts = Setting.Accounts.Collection.Select(a => a.Id).ToArray();
            return s => s.FavoritedUsers != null && accounts.Any(a => s.FavoritedUsers.Contains(a));
        }

        public override string GetBooleanSqlQuery()
        {
            return "exists (select UserId from Favorites where StatusId = status.id intersects " +
                   Setting.Accounts.Collection
                          .Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                          .JoinString(",").EnumerationToSelectClause()
                   + ")";
        }

        public override string ToQuery()
        {
            return "favorited";
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
            var accounts = Setting.Accounts.Collection.Select(a => a.Id).ToArray();
            return s => s.RetweetedUsers != null && accounts.Any(a => s.RetweetedUsers.Contains(a));
        }

        public override string GetBooleanSqlQuery()
        {
            return "exists (select UserId from Retweets where StatusId = status.id intersects " +
                   Setting.Accounts.Collection
                          .Select(a => a.Id.ToString(CultureInfo.InvariantCulture))
                          .JoinString(",").EnumerationToSelectClause()
                   + ")";
        }

        public override string ToQuery()
        {
            return "retweeted";
        }
    }

    public sealed class StatusIsRetweet : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Boolean; }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return _ => _.RetweetedOriginal != null;
        }

        public override string GetBooleanSqlQuery()
        {
            return "RetweetOriginalId is not null";
        }

        public override string ToQuery()
        {
            return "retweet";
        }
    }

}
