namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// User events
    /// </summary>
    /// <remarks>
    /// This message indicates: events about twitter users
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public class StreamWithheld : StreamMessage
    {
        public StreamWithheld(long userId, string[] withheldCountries, string timestampMs)
            : base(timestampMs)
        {
            Type = WithheldType.User;
            UserId = userId;
            Id = userId;
            WithheldCountryCodes = withheldCountries;
        }

        public StreamWithheld(long userId, long tweetId, string[] withheldCountries, string timestampMs)
            : base(timestampMs)
        {
            Type = WithheldType.Status;
            UserId = userId;
            Id = tweetId;
            WithheldCountryCodes = withheldCountries;
        }

        /// <summary>
        /// Withheld type
        /// </summary>
        public WithheldType Type { get; }

        /// <summary>
        /// Target user id
        /// </summary>
        public long UserId { get; }

        /// <summary>
        /// Target id<para/>
        /// If this message represents user_withheld, this property returns user_id.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Target country codes
        /// </summary>
        public string[] WithheldCountryCodes { get; }
    }

    public enum WithheldType
    {
        Status,
        User
    }
}
