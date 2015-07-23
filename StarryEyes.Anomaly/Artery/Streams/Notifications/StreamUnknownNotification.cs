using System;

namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Unknown events
    /// </summary>
    /// <remarks>
    /// This notification indicates: Anomaly could not handle this event.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams
    /// </remarks>
    public sealed class StreamUnknownNotification : StreamNotification
    {
        private readonly string _eventName;
        private readonly string _json;

        public StreamUnknownNotification(string eventName, string json)
            : base(DateTime.Now) // Unknown element may not have a timestamp.
        {
            _eventName = eventName;
            _json = json;
        }

        /// <summary>
        /// (Maybe) event name
        /// </summary>
        public string EventName
        {
            get { return _eventName; }
        }

        /// <summary>
        /// Original json representation
        /// </summary>
        public string Json
        {
            get { return _json; }
        }
    }
}
