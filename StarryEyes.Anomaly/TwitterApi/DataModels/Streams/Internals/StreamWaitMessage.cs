using Cadena;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Internals
{
    public sealed class StreamWaitMessage : InternalMessage
    {
        public IOAuthCredential Credential { get; set; }

        public long WaitSec { get; set; }

        public StreamWaitMessage(IOAuthCredential credential, long waitSec)
        {
            Credential = credential;
            WaitSec = waitSec;
        }
    }
}