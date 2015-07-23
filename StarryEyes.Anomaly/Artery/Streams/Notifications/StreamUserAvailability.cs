namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// User events
    /// </summary>
    /// <remarks>
    /// This notification indicates: change availability of specified user.
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public sealed class StreamUserAvailability : StreamNotification
    {
        private readonly long _userId;
        private readonly AvailabilityChanges _availability;

        public StreamUserAvailability(long userId, AvailabilityChanges availability, string timestampMs)
            : base(timestampMs)
        {
            _userId = userId;
            _availability = availability;
        }

        /// <summary>
        /// Target user id
        /// </summary>
        public long UserId
        {
            get { return _userId; }
        }

        /// <summary>
        /// Availability flag
        /// </summary>
        public AvailabilityChanges AvailabilityChanges
        {
            get { return _availability; }
        }
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
