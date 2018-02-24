using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cadena.Data;
using StarryEyes.Casket.DatabaseModels;
using StarryEyes.Helpers;

namespace StarryEyes.Filters.Expressions.Values.Statuses
{
    public sealed class StatusTo : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
                yield return FilterExpressionType.String;
                yield return FilterExpressionType.Set;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return s => s.StatusType == StatusType.Tweet || s.Recipient == null
                ? s.GetOriginal().InReplyToUserId.GetValueOrDefault(-1)
                : s.Recipient.Id;
        }

        public override string GetNumericSqlQuery()
        {
            return Coalesce("InReplyToOrRecipientUserId", -1);
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return s => s.StatusType == StatusType.Tweet || s.Recipient == null
                ? s.GetOriginal().InReplyToScreenName ?? String.Empty
                : s.Recipient.ScreenName;
        }

        public override string GetStringSqlQuery()
        {
            return Coalesce("InReplyToOrRecipientScreenName", "");
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return s => s.StatusType == StatusType.Tweet || s.Recipient == null
                ? FilterSystemUtil.InReplyToUsers(s.GetOriginal()).ToList()
                : new[] { s.Recipient.Id }.ToList();
        }

        public override string GetSetSqlQuery()
        {
            var userMention = ((int)EntityType.UserMentions).ToString(CultureInfo.InvariantCulture);
            return "(select InReplyToOrRecipientUserId union " +
                   "select UserId from StatusEntity where " +
                   "ParentId = status.Id and " +
                   "EntityType = " + userMention + " and " +
                   "UserId is not null)";
        }

        public override string ToQuery()
        {
            return "to";
        }
    }

    public sealed class StatusFavorites : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Set;
                yield return FilterExpressionType.Numeric;
            }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return s => s.FavoritedUsers ?? new long[0];
        }

        public override string GetSetSqlQuery()
        {
            return "(select UserId from Favorites where StatusId = status.Id)";
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return s => (s.FavoritedUsers ?? new long[0]).Length;
        }

        public override string GetNumericSqlQuery()
        {
            return "(select count(Id) from Favorites where StatusId = status.Id)";
        }

        public override string ToQuery()
        {
            return "favs";
        }
    }

    public sealed class StatusRetweets : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Set;
                yield return FilterExpressionType.Numeric;
            }
        }

        public override Func<TwitterStatus, IReadOnlyCollection<long>> GetSetValueProvider()
        {
            return s => s.RetweetedUsers ?? new long[0];
        }

        public override string GetSetSqlQuery()
        {
            return "(select UserId from Retweets where StatusId = status.Id)";
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return s => (s.RetweetedUsers ?? new long[0]).Length;
        }

        public override string GetNumericSqlQuery()
        {
            return "(select count(Id) from Retweets where StatusId = status.Id)";
        }

        public override string ToQuery()
        {
            return "retweets";
        }
    }
}