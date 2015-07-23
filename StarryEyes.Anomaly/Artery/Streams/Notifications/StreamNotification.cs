using System;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Base class of stream notification messages.
    /// </summary>
    public abstract class StreamNotification
    {
        private readonly DateTime _timestamp;

        private static readonly DateTime SerialTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Initialize stream notification.
        /// </summary>
        /// <param name="timestampMs">serial timestamp (millisec, from 1970/01/01 00:00:00)</param>
        protected StreamNotification(string timestampMs)
            : this(timestampMs.ParseLong())
        {
        }

        /// <summary>
        /// Initialize stream notification.
        /// </summary>
        /// <param name="timestampMs">serial timestamp (millisec, from 1970/01/01 00:00:00)</param>
        protected StreamNotification(long timestampMs)
        {
            _timestamp = SerialTime.AddMilliseconds(timestampMs).ToLocalTime();
        }

        /// <summary>
        /// Initialize stream notification.
        /// </summary>
        /// <param name="timestamp">timestamp</param>
        protected StreamNotification(DateTime timestamp)
        {
            _timestamp = timestamp;
        }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }
    }
}
