using System;
using Cadena.Data;
using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("Status")]
    public class DatabaseStatus
    {
        [DbPrimaryKey]
        public long Id { get; set; }

        public long BaseId { get; set; }

        public long? RetweetId { get; set; }

        public long? RetweetOriginalId { get; set; }

        public long? QuoteId { get; set; }

        public StatusType StatusType { get; set; }

        public long UserId { get; set; }

        public long BaseUserId { get; set; }

        public long? RetweeterId { get; set; }

        public long? RetweetOriginalUserId { get; set; }

        public long? QuoteUserId { get; set; }

        public string EntityAidedText { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }

        [DbOptional]
        public string BaseSource { get; set; }

        [DbOptional]
        public string Source { get; set; }

        public long? InReplyToStatusId { get; set; }

        public long? InReplyToOrRecipientUserId { get; set; }

        [DbOptional]
        public string InReplyToOrRecipientScreenName { get; set; }

        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        public int? DisplayTextRangeBegin { get; set; }

        public int? DisplayTextRangeEnd { get; set; }
    }
}