
namespace StarryEyes.Anomaly.Artery.Streams.Notifications.Warnings
{
    /// <summary>
    /// Too many follows
    /// </summary>
    /// <remarks>
    /// This notification indicates: too many follows to delivering tweets.
    /// Content indicates target user_id
    /// 
    /// This element is supported by: user streams, site streams
    /// </remarks>
    public sealed class StreamTooManyFollowsWarning : StreamWarning<long>
    {
        public StreamTooManyFollowsWarning(string code, string message, long content, string timestampMs)
            : base(code, message, content, timestampMs) { }
    }
}
