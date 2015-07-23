namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Status deletion notices
    /// </summary>
    /// <remarks>
    /// This notification indicates: specified tweet was deleted.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamDelete : StreamNotification
    {
        private readonly long _id;
        private readonly long _userId;

        public StreamDelete(long id, long userId, string timestampMs)
            : base(timestampMs)
        {
            _id = id;
            _userId = userId;
        }

        /// <summary>
        /// Id of deleted tweet
        /// </summary>
        public long Id { get { return _id; } }

        /// <summary>
        /// Id of author of deleted tweet
        /// </summary>
        public long UserId { get { return _userId; } }
    }
}
