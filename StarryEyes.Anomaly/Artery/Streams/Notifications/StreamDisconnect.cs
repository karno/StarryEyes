
namespace StarryEyes.Anomaly.Artery.Streams.Notifications
{
    /// <summary>
    /// Disconnect notices
    /// </summary>
    /// <remarks>
    /// This notification indicates: current stream is disconnected from twitter.
    /// 
    /// This element is supported by: (generic) streams, user streams, site streams.
    /// </remarks>
    public sealed class StreamDisconnect : StreamNotification
    {
        private readonly DisconnectCode _code;
        private readonly string _streamName;
        private readonly string _reason;

        public StreamDisconnect(DisconnectCode code, string streamName,
            string reason, string timestampMs)
            : base(timestampMs)
        {
            _code = code;
            _streamName = streamName;
            _reason = reason;
        }

        /// <summary>
        /// Disconnection code<para />
        /// You can cast this value to int if required.
        /// </summary>
        public DisconnectCode Code { get { return _code; } }

        /// <summary>
        /// Disconnected stream name
        /// </summary>
        public string StreamName { get { return _streamName; } }

        /// <summary>
        /// Disconnection reason
        /// </summary>
        public string Reason { get { return _reason; } }
    }

    public enum DisconnectCode
    {
        /// <summary>
        /// Feed was shutdown.
        /// </summary>
        Shutdown = 1,
        /// <summary>
        /// The same endpoint was connected too many times.
        /// </summary>
        DuplicateStream = 2,
        /// <summary>
        /// Control streams was used to close a stream. (SiteStreams)
        /// </summary>
        ControlRequest = 3,
        /// <summary>
        /// The client was reading too slowly and was disconnected by the server.
        /// </summary>
        Stall = 4,
        /// <summary>
        /// The client appeared to have initiated a disconnect.
        /// </summary>
        Normal = 5,
        /// <summary>
        /// An oauth token was revoked.
        /// </summary>
        TokenRevoked = 6,
        /// <summary>
        /// The same credentials were used to connect a new stream.
        /// </summary>
        AdminLogout = 7,
        /// <summary>
        /// This code is reserved fof use inside of Twitter Co.
        /// </summary>
        Internal = 8,
        /// <summary>
        /// The streams connected with a negative count parameter and was
        /// disconnected after all backfill was delivered.
        /// </summary>
        MaxMessageLimit = 9,
        /// <summary>
        /// An internal issue disconnected the stream.
        /// </summary>
        StreamException = 10,
        /// <summary>
        /// An internal issue disconnected the stream.
        /// </summary>
        BrokerStall = 11,
        /// <summary>
        /// The host the stream was connected to became overloaded and streams were
        /// disconnected to balance load.
        /// Reconnect as usual.
        /// </summary>
        ShedLoad = 12,
    }
}
