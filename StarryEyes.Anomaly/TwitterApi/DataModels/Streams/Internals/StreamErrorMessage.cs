using System.Net;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Internals
{
    public sealed class StreamErrorMessage : InternalMessage
    {
        public IOAuthCredential Credential { get; set; }

        public HttpStatusCode Code { get; }

        public TwitterErrorCode? TwitterErrorCode { get; set; }

        public StreamErrorMessage(IOAuthCredential credential, HttpStatusCode hcode, TwitterErrorCode? tcode)
        {
            Credential = credential;
            Code = hcode;
            TwitterErrorCode = tcode;
        }
    }
}
