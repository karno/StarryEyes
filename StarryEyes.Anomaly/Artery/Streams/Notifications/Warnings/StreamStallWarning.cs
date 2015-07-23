
namespace StarryEyes.Anomaly.Artery.Streams.Notifications.Warnings
{
    /// <summary>
    /// Stall warnings
    /// </summary>
    /// <remarks>
    /// This notification indicates: delivering queue fill rate.
    /// if queue up to full, stream is automatically disconnected.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamStallWarning : StreamWarning<int>
    {
        public StreamStallWarning(string code, string message, int content, string timestampMs)
            : base(code, message, content, timestampMs) { }
    }
}
