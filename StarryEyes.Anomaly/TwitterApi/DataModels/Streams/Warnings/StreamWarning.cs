
namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Warnings
{
    /// <summary>
    /// Describes general twitter stream warning.
    /// </summary>
    /// <remarks>
    /// This element is supported by: (generic) streams, user streams, site streams
    /// </remarks>
    public abstract class StreamWarning<T> : StreamMessage
    {
        protected StreamWarning(string code, string message, T content, string timestampMs)
            : base(timestampMs)
        {
            Code = code;
            Message = message;
            Content = content;
        }

        public string Code { get; }

        public string Message { get; }

        public T Content { get; }
    }
}
