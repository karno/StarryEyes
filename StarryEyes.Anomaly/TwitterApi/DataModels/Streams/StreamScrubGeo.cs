namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// Location deletion notices
    /// </summary>
    /// <remarks>
    /// This message indicates: delete location information from (range-)specified statuses.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamScrubGeo : StreamMessage
    {
        public StreamScrubGeo(long userId, long upToStatusId, string timestampMs)
            : base(timestampMs)
        {
            UserId = userId;
            UpToStatusId = upToStatusId;
        }

        /// <summary>
        /// Id of target user
        /// </summary>
        public long UserId { get; }

        /// <summary>
        /// the ending Id of target range of statuses
        /// </summary>
        public long UpToStatusId { get; }
    }
}
