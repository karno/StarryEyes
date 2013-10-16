using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;

namespace StarryEyes.Anomaly.TwitterApi
{
    public class TwitterApiException : WebException
    {
        public static async Task<TwitterApiException> Convert(WebException wex)
        {
            if (wex == null)
            {
                throw new ArgumentNullException("wex");
            }
            var stream = wex.Response != null ? wex.Response.GetResponseStream() : null;
            if (stream != null)
            {
                using (var sr = new StreamReader(stream))
                {
                    var resp = DynamicJson.Parse(await sr.ReadToEndAsync());
                    if (resp.errors() && resp.errors.code() && resp.errors.message())
                    {
                        return new TwitterApiException(
                            resp.errors.message,
                            (int)resp.errors.code,
                            wex);
                    }
                }
            }
            return new TwitterApiException(wex);
        }

        private TwitterApiException(string message, int code, WebException innerException)
            : base(message, innerException)
        {
            this.ErrorCode = code;
        }

        private TwitterApiException(WebException innerException)
            : base(innerException.Message, innerException) { }

        public int? ErrorCode { get; private set; }

        public override string ToString()
        {
            if (ErrorCode.HasValue)
            {
                return ErrorCode.Value + ": " + Message;
            }
            return InnerException.ToString();
        }
    }

    public class TwitterApiExceptionHandler : DelegatingHandler
    {
        public TwitterApiExceptionHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            WebException thrownWex;
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (WebException wex)
            {
                thrownWex = wex;
            }
            throw await TwitterApiException.Convert(thrownWex);
        }
    }
}
