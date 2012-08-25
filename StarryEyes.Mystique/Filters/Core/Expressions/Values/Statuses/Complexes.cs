using System;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Filters.Core.Expressions.Values.Statuses
{
    public sealed class User : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric, KQExpressionType.String }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().User.Id;
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().User.ScreenName;
        }

        public override string ToQuery()
        {
            return "user";
        }
    }

    public sealed class Retweeter : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric, KQExpressionType.String }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.Id : -1;
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.RetweetedOriginal != null ? status.User.ScreenName : String.Empty;
        }

        public override string ToQuery()
        {
            return "retweeter";
        }
    }

    public sealed class StatusInReplyTo : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric, KQExpressionType.String }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.GetOriginal().InReplyToStatusId.GetValueOrDefault(-1);
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.GetOriginal().InReplyToScreenName ?? string.Empty;
        }

        public override string ToQuery()
        {
            return "in_reply_to";
        }
    }

    public sealed class StatusTo : ValueBase
    {
        public override KQExpressionType[] TransformableTypes
        {
            get { return new[] { KQExpressionType.Numeric, KQExpressionType.String }; }
        }

        public override long GetNumericValue(TwitterStatus status)
        {
            return status.StatusType == StatusType.Tweet ?
                status.GetOriginal().InReplyToUserId.GetValueOrDefault(-1) :
                status.Recipient.Id;
        }

        public override string GetStringValue(TwitterStatus status)
        {
            return status.StatusType == StatusType.Tweet ?
                status.GetOriginal().InReplyToScreenName ?? string.Empty : 
                status.Recipient.ScreenName;
        }

        public override string ToQuery()
        {
            return "to";
        }
    }
}
