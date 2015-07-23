using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.Artery.Streams.Notifications.Events
{
    /// <summary>
    /// Describes event from stream
    /// </summary>
    /// <typeparam name="TObject">type of target object</typeparam>
    /// <typeparam name="TEvent">type of event enumeration</typeparam>
    public class StreamEvent<TObject, TEvent> : StreamNotification where TEvent : struct,IConvertible
    {
        private readonly DateTime _createdAt;
        private readonly TwitterUser _source;
        private readonly TwitterUser _target;
        private readonly TObject _targetObject;
        private readonly TEvent _event;
        private readonly string _rawEvent;

        protected StreamEvent(TwitterUser source, TwitterUser target,
            TObject targetObject, TEvent @event, string rawEvent, DateTime createdAt)
            : base(createdAt)
        {
            _source = source;
            _target = target;
            _targetObject = targetObject;
            _event = @event;
            _rawEvent = rawEvent;
            _createdAt = createdAt;
        }

        /// <summary>
        /// Get source user of this event
        /// </summary>
        public TwitterUser Source
        {
            get { return _source; }
        }

        /// <summary>
        /// Get target user of this event
        /// </summary>
        public TwitterUser Target
        {
            get { return _target; }
        }

        /// <summary>
        /// Get target object of this event
        /// </summary>
        public TObject TargetObject
        {
            get { return _targetObject; }
        }

        /// <summary>
        /// Get enum-typed event
        /// </summary>
        public TEvent Event
        {
            get { return _event; }
        }

        /// <summary>
        /// Get raw-string event
        /// </summary>
        public string RawEvent
        {
            get { return _rawEvent; }
        }

        /// <summary>
        /// Time of this event raised
        /// </summary>
        public DateTime CreatedAt
        {
            get { return _createdAt; }
        }
    }
}
