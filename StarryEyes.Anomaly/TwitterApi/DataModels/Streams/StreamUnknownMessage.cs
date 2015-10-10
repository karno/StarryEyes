using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams
{
    /// <summary>
    /// Unknown events
    /// </summary>
    /// <remarks>
    /// This message indicates: Anomaly could not handle this event.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams
    /// </remarks>
    public sealed class StreamUnknownMessage : StreamMessage
    {
        public StreamUnknownMessage(string eventName, string json)
            : base(DateTime.Now) // Unknown element may not have a timestamp.
        {
            EventName = eventName;
            Json = json;
        }

        /// <summary>
        /// (Maybe) event name
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// Original json representation
        /// </summary>
        public string Json { get; }
    }
}
