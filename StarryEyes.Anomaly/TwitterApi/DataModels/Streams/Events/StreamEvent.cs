using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Events
{
    /// <summary>
    /// Describes event from stream
    /// </summary>
    /// <typeparam name="TObject">type of target object</typeparam>
    /// <typeparam name="TEvent">type of event enumeration</typeparam>
    public class StreamEvent<TObject, TEvent> : StreamMessage where TEvent : struct, IConvertible
    {
        protected StreamEvent(TwitterUser source, TwitterUser target,
            TObject targetObject, TEvent @event, string rawEvent, DateTime createdAt)
            : base(createdAt)
        {
            Source = source;
            Target = target;
            TargetObject = targetObject;
            Event = @event;
            RawEvent = rawEvent;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Get source user of this event
        /// </summary>
        public TwitterUser Source { get; }

        /// <summary>
        /// Get target user of this event
        /// </summary>
        public TwitterUser Target { get; }

        /// <summary>
        /// Get target object of this event
        /// </summary>
        public TObject TargetObject { get; }

        /// <summary>
        /// Get enum-typed event
        /// </summary>
        public TEvent Event { get; }

        /// <summary>
        /// Get raw-string event
        /// </summary>
        public string RawEvent { get; }

        /// <summary>
        /// Time of this event raised
        /// </summary>
        public DateTime CreatedAt { get; }
    }
}
