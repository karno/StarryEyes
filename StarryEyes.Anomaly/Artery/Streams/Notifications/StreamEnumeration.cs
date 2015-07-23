
using System;

namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Friends lists
    /// </summary>
    /// <remarks>
    /// This notification indicates: friends list id.
    /// 
    /// This element is supported by: user streams
    /// </remarks>
    public sealed class StreamEnumeration : StreamNotification
    {
        private readonly long[] _enumeration;

        public StreamEnumeration(long[] enumeration)
            : base(DateTime.Now) // stream enumeration is not have a timestamp
        {
            _enumeration = enumeration;
        }

        /// <summary>
        /// Enumerated Ids
        /// </summary>
        public long[] Enumeration { get { return _enumeration; } }
    }
}
