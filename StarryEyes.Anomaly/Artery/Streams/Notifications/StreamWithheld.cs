namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// User events
    /// </summary>
    /// <remarks>
    /// This notification indicates: events about twitter users
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public class StreamWithheld : StreamNotification
    {
        private readonly long _userId;

        private readonly long _id;

        private readonly WithheldType _withheldType;

        private readonly string[] _withheldCountryCodes;

        public StreamWithheld(long userId, string[] withheldCountries, string timestampMs)
            : base(timestampMs)
        {
            _withheldType = WithheldType.User;
            _userId = userId;
            _id = userId;
            _withheldCountryCodes = withheldCountries;
        }

        public StreamWithheld(long userId, long tweetId, string[] withheldCountries, string timestampMs)
            : base(timestampMs)
        {
            _withheldType = WithheldType.Status;
            _userId = userId;
            _id = tweetId;
            _withheldCountryCodes = withheldCountries;
        }

        /// <summary>
        /// Withheld type
        /// </summary>
        public WithheldType Type
        {
            get { return _withheldType; }
        }

        /// <summary>
        /// Target user id
        /// </summary>
        public long UserId
        {
            get { return _userId; }
        }

        /// <summary>
        /// Target id<para/>
        /// If this message represents user_withheld, this property returns user_id.
        /// </summary>
        public long Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Target country codes
        /// </summary>
        public string[] WithheldCountryCodes
        {
            get { return _withheldCountryCodes; }
        }
    }

    public enum WithheldType
    {
        Status,
        User
    }
}
