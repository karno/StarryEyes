using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Filters.Expressions.Values.Statuses
{
    public sealed class StatusId : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get { yield return FilterExpressionType.Numeric; }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().Id;
        }

        public override string GetNumericSqlQuery()
        {
            return "BaseId";
        }

        public override string ToQuery()
        {
            return "id";
        }
    }

    public sealed class StatusInReplyTo : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Numeric;
            }
        }

        public override Func<TwitterStatus, long> GetNumericValueProvider()
        {
            return _ => _.GetOriginal().InReplyToStatusId.GetValueOrDefault(-1);
        }

        public override string GetNumericSqlQuery()
        {
            // in database entity, in_reply_to_status_id in retweeted status indicates
            // replying status mentioned from original status.
            return Coalesce("InReplyToStatusId", -1);
        }

        public override string ToQuery()
        {
            return "in_reply_to";
        }
    }
}
