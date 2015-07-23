
namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Limit notices
    /// </summary>
    /// <remarks>
    /// This notification indicates: timeline speed is reached to delivery limit.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamLimit : StreamNotification
    {
        private readonly long _undeliveredCount;

        public StreamLimit(long undeliveredCount, string timestampMs)
            : base(timestampMs)
        {
            _undeliveredCount = undeliveredCount;
        }

        /// <summary>
        /// Count of undelivered tweets
        /// </summary>
        public long UndeliveredCount { get { return _undeliveredCount; } }
    }
}
