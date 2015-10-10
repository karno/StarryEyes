namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// Limit notices
    /// </summary>
    /// <remarks>
    /// This message indicates: timeline speed is reached to delivery limit.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamLimit : StreamMessage
    {
        public StreamLimit(long undeliveredCount, string timestampMs)
            : base(timestampMs)
        {
            UndeliveredCount = undeliveredCount;
        }

        /// <summary>
        /// Count of undelivered tweets
        /// </summary>
        public long UndeliveredCount { get; }
    }
}
