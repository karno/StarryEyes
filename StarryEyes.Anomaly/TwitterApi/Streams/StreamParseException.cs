using System;
using System.Runtime.Serialization;

namespace StarryEyes.Anomaly.TwitterApi.Streams
{
    [Serializable]
    public class StreamParseException : Exception
    {
        private readonly string _received;

        public StreamParseException(string received)
        {
            _received = received;
        }

        public StreamParseException(string message, string received)
            : base(message)
        {
            _received = received;
        }

        public StreamParseException(string message, string received, Exception inner)
            : base(message, inner)
        {
            _received = received;
        }

        protected StreamParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _received = info.GetString("_received");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_received", _received);
        }

        public string ReceivedMessage
        {
            get { return _received; }
        }
    }
}
