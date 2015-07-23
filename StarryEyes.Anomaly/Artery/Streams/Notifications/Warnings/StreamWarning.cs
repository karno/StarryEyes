
namespace StarryEyes.Anomaly.Artery.Streams.Notifications.Warnings
{
    /// <summary>
    /// Describes general twitter stream warning.
    /// </summary>
    /// <remarks>
    /// This element is supported by: (generic) streams, user streams, site streams
    /// </remarks>
    public abstract class StreamWarning<T> : StreamNotification
    {
        protected StreamWarning(string code, string message, T content, string timestampMs)
            : base(timestampMs)
        {
            _code = code;
            _message = message;
            _content = content;
        }

        private readonly string _code;

        private readonly string _message;

        private readonly T _content;

        public string Code
        {
            get { return _code; }
        }

        public string Message
        {
            get { return _message; }
        }

        public T Content
        {
            get { return _content; }
        }
    }
}
