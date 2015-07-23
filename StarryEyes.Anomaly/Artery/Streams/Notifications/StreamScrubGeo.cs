namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Location deletion notices
    /// </summary>
    /// <remarks>
    /// This notification indicates: delete location information from (range-)specified statuses.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamScrubGeo : StreamNotification
    {
        private readonly long _userId;
        private readonly long _upToStatusId;

        public StreamScrubGeo(long userId, long upToStatusId, string timestampMs)
            : base(timestampMs)
        {
            _userId = userId;
            _upToStatusId = upToStatusId;
        }

        /// <summary>
        /// Id of target user
        /// </summary>
        public long UserId
        {
            get { return _userId; }
        }

        /// <summary>
        /// the ending Id of target range of statuses
        /// </summary>
        public long UpToStatusId
        {
            get { return _upToStatusId; }
        }
    }
}
