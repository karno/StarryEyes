namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// Status deletion notices
    /// </summary>
    /// <remarks>
    /// This message indicates: specified tweet was deleted.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamDelete : StreamMessage
    {
        public StreamDelete(long id, long userId, string timestampMs)
            : base(timestampMs)
        {
            Id = id;
            UserId = userId;
        }

        /// <summary>
        /// Id of deleted tweet
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Id of author of deleted tweet
        /// </summary>
        public long UserId { get; }
    }
}
