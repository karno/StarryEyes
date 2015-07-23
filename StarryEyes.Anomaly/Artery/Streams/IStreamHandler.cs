using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Artery.Streams.Notifications;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.Artery.Streams
{
    public interface IStreamHandler
    {
        void OnStatus(TwitterStatus status);

        void OnNotification(StreamNotification notification);

        void OnException(StreamParseException exception);
    }

    public sealed class StreamHandler : IStreamHandler
    {
        private readonly Action<TwitterStatus> _statusHandler;
        private readonly Action<StreamParseException> _exceptionHandler;
        private readonly Dictionary<Type, Action<StreamNotification>> _notificationHandlers;

        public static StreamHandler Create([NotNull] Action<TwitterStatus> statusHandler,
           [NotNull] Action<StreamParseException> exceptionHandler)
        {
            if (statusHandler == null) throw new ArgumentNullException("statusHandler");
            if (exceptionHandler == null) throw new ArgumentNullException("exceptionHandler");
            return new StreamHandler(statusHandler, exceptionHandler);
        }

        private StreamHandler(Action<TwitterStatus> statusHandler, Action<StreamParseException> exceptionHandler)
        {
            _statusHandler = statusHandler;
            _exceptionHandler = exceptionHandler;
            _notificationHandlers = new Dictionary<Type, Action<StreamNotification>>();
        }

        public void OnStatus(TwitterStatus status)
        {
            _statusHandler(status);
        }

        public void OnException(StreamParseException exception)
        {
            _exceptionHandler(exception);
        }

        public void OnNotification(StreamNotification notification)
        {
            Action<StreamNotification> value;
            var type = notification.GetType();
            lock (_notificationHandlers)
            {
                do
                {
                    if (_notificationHandlers.TryGetValue(type, out value))
                    {
                        break;
                    }
                    type = type.BaseType;
                } while (type != null && typeof(StreamNotification).IsAssignableFrom(type));
            }
            if (value != null)
            {
                value(notification);
            }
        }

        public StreamHandler AddHandler<T>(Action<T> handler) where T : StreamNotification
        {
            lock (_notificationHandlers)
            {
                if (_notificationHandlers.ContainsKey(typeof(T)))
                {
                    throw new ArgumentException("a handler for the type " + typeof(T).Name + " is already registered.");
                }
                _notificationHandlers.Add(typeof(T), i => handler((T)i));
            }
            return this;
        }
    }
}
