using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Status")]
    public class DatabaseStatus : DbModelBase
    {
        [DbPrimaryKey]
        public long Id { get; set; }

        public StatusType StatusType { get; set; }

        public long UserId { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }

        [DbOptional]
        public string Source { get; set; }

        public long? InReplyToStatusId { get; set; }

        public long? InReplyToUserId { get; set; }

        [DbOptional]
        public string InReplyToScreenName { get; set; }

        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        public long? RetweetOriginalId { get; set; }

        public long? RecipientId { get; set; }
    }
}
