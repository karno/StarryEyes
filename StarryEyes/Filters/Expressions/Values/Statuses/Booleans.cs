using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StarryEyes.Anomaly.TwitterApi.DataModels;

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

    public sealed class StatusHasMedia : ValueBase
    {
        public override IEnumerable<FilterExpressionType> SupportedTypes
        {
            get
            {
                yield return FilterExpressionType.Boolean;
            }
        }

        public override Func<TwitterStatus, bool> GetBooleanValueProvider()
        {
            return st =>
            {
                foreach (var entity in st.Entities)
                {
                    if (entity.EntityType == EntityType.Media) return true;
                }
                return false;
            };
        }

        public override string GetBooleanSqlQuery()
        {
            return "(SELECT COUNT(Id) FROM StatusEntity WHERE StatusEntity.ParentId = Status.Id AND StatusEntity.EntityType = 0) > 0";
        }

        public override string ToQuery()
        {
            return "has_media";
        }
    }
}
