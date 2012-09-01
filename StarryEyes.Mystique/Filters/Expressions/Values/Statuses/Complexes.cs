using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Statuses
{
    public sealed class StatusInReplyTo : ValueBase
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
            return _ => _.GetOriginal().InReplyToStatusId.GetValueOrDefault(-1);
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().InReplyToScreenName ?? string.Empty;
        }

        public override string ToQuery()
        {
            return "in_reply_to";
        }
    }

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
            return _ => _.StatusType == StatusType.Tweet ?
                _.GetOriginal().InReplyToUserId.GetValueOrDefault(-1) :
                _.Recipient.Id;
        }

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.StatusType == StatusType.Tweet ?
                _.GetOriginal().InReplyToScreenName ?? string.Empty : 
                _.Recipient.ScreenName;
        }

        public override Func<TwitterStatus, ICollection<long>> GetSetValueProvider()
        {
            return _ => _.StatusType == StatusType.Tweet ?
                FilterSystemUtil.InReplyToUsers(_.GetOriginal()).ToList() :
                new[] { _.Recipient.Id }.ToList();
        }

        public override string ToQuery()
        {
            return "to";
        }
    }
}
