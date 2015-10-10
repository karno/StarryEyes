namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// User events
    /// </summary>
    /// <remarks>
    /// This message indicates: change availability of specified user.
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public sealed class StreamUserAvailability : StreamMessage
    {
        public StreamUserAvailability(long userId, AvailabilityChanges availability, string timestampMs)
            : base(timestampMs)
        {
            UserId = userId;
            AvailabilityChanges = availability;
        }

        /// <summary>
        /// Target user id
        /// </summary>
        public long UserId { get; }

        /// <summary>
        /// Availability flag
        /// </summary>
        public AvailabilityChanges AvailabilityChanges { get; }
    }

    public enum AvailabilityChanges
    {
        Protected,
        Unprotected,
        Suspended,
        Unsuspended,
        Deleted,
        Undeleted
    }
}
