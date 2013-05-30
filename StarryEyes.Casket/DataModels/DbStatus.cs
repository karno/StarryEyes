using System;

namespace StarryEyes.Casket.DataModels
{
    public class DbStatus
    {
        /// <summary>
        /// Numerical ID of this tweet/message.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Flags of this tweet is direct message or not.
        /// </summary>
        public bool IsDirectMessage { get; set; }

        /// <summary>
        /// User id.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Body of this tweet/message.
        /// </summary>
        public string Text { get; set; }

        // todo 
        // public string NGramText { get; set; }

        /// <summary>
        /// Created at of this tweet/message.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #region Status

        /// <summary>
        /// Source of this tweet. (a.k.a. via, from, ...)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Status ID which replied this tweet.
        /// </summary>
        public long? InReplyToStatusId { get; set; }

        /// <summary>
        /// User ID which replied this tweet.
        /// </summary>
        public long? InReplyToUserId { get; set; }

        /// <summary>
        /// User screen name which replied this tweet.
        /// </summary>
        public string InReplyToScreenName { get; set; }

        /// <summary>
        /// Tweet Id which retweeted as this.
        /// </summary>
        public long? RetweetedOriginalId { get; set; }

        /// <summary>
        /// Geographic point, represents longitude.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Geographic point, represents latitude.
        /// </summary>
        public double? Latitude { get; set; }

        #endregion

        #region Direct Message

        public long? RecipientUserId { get; set; }

        #endregion

        public override bool Equals(object obj)
        {
            var casted = obj as DbStatus;
            return casted != null && this.Id == casted.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
