using System;
using StarryEyes.SweetLady.DataModel;
using System.Collections.Generic;

namespace StarryEyes.Mystique.Filters.Expressions.Values.Statuses
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

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.GetOriginal().User.ScreenName;
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

        public override Func<TwitterStatus, string> GetStringValueProvider()
        {
            return _ => _.RetweetedOriginal != null ? _.User.ScreenName : String.Empty;
        }

        public override string ToQuery()
        {
            return "retweeter";
        }
    }

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

        public override string ToQuery()
        {
            return "to";
        }
    }
}
