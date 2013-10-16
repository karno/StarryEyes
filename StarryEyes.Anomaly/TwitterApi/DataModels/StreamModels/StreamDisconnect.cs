
namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamDisconnect
    {
        public DisconnectCode Code { get; set; }

        public string StreamName { get; set; }

        public string Reason { get; set; }
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
