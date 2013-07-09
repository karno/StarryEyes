using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Casket.DatabaseModels
{
    public class DatabaseStatus
    {
        public long Id { get; set; }

        public StatusType StatusType { get; set; }

        public long UserId { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Source { get; set; }

        public long? InReplyToStatusId { get; set; }

        public long? InReplyToUserId { get; set; }

        public string InReplyToScreenName { get; set; }

        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        public long? RetweetOriginalId { get; set; }

        public long? Recipient { get; set; }
    }
}
