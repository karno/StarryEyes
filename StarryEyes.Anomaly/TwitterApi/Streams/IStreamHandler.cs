using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.Streams;

namespace StarryEyes.Anomaly.TwitterApi.Streams
{

    public interface IStreamHandler
    {
        void OnStatus(TwitterStatus status);

        void OnMessage(StreamMessage notification);

        void OnException(StreamParseException exception);

        void Log(string log);
    }

    public sealed class StreamHandler : IStreamHandler
    {
        private readonly Action<TwitterStatus> _statusHandler;
        private readonly Action<StreamParseException> _exceptionHandler;
        private readonly Dictionary<Type, Action<StreamMessage>> _notificationHandlers;

        [CanBeNull]
        private readonly Action<string> _logHandler;

        public static StreamHandler Create([NotNull] Action<TwitterStatus> statusHandler,
            [NotNull] Action<StreamParseException> exceptionHandler)
        {
            if (statusHandler == null) throw new ArgumentNullException(nameof(statusHandler));
            if (exceptionHandler == null) throw new ArgumentNullException(nameof(exceptionHandler));
            return new StreamHandler(statusHandler, exceptionHandler, null);
        }

        public static StreamHandler Create([NotNull] Action<TwitterStatus> statusHandler,
            [NotNull] Action<StreamParseException> exceptionHandler, [NotNull] Action<string> logHandler)
        {
            if (statusHandler == null) throw new ArgumentNullException(nameof(statusHandler));
            if (exceptionHandler == null) throw new ArgumentNullException(nameof(exceptionHandler));
            if (logHandler == null) throw new ArgumentNullException(nameof(logHandler));
            return new StreamHandler(statusHandler, exceptionHandler, logHandler);
        }

        private StreamHandler([NotNull] Action<TwitterStatus> statusHandler,
            [NotNull] Action<StreamParseException> exceptionHandler, [CanBeNull] Action<string> logHandler)
        {
            if (statusHandler == null) throw new ArgumentNullException(nameof(statusHandler));
            if (exceptionHandler == null) throw new ArgumentNullException(nameof(exceptionHandler));
            _statusHandler = statusHandler;
            _exceptionHandler = exceptionHandler;
            _logHandler = logHandler;
            _notificationHandlers = new Dictionary<Type, Action<StreamMessage>>();
        }

        public void OnStatus(TwitterStatus status)
        {
            _statusHandler(status);
        }

        public void OnException(StreamParseException exception)
        {
            _exceptionHandler(exception);
        }

        public void OnMessage(StreamMessage notification)
        {
            Action<StreamMessage> value;
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
                } while (type != null && typeof(StreamMessage).IsAssignableFrom(type));
            }
            // invoke if value is not null
            value?.Invoke(notification);
        }

        public void Log(string log)
        {
            _logHandler?.Invoke("[" + DateTime.Now.ToString("yy/MM/dd hh:mm:ss tt [zz]") + "]" + log);
        }

        public StreamHandler AddHandler<T>(Action<T> handler) where T : StreamMessage
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
