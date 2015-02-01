using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;

namespace StarryEyes.Anomaly.TwitterApi
{
    public class TwitterApiException : Exception
    {
        public TwitterApiException(HttpStatusCode statusCode, string message, int code)
            : base(message)
        {
            StatusCode = statusCode;
            TwitterErrorCode = code;
        }

        public TwitterApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public int? TwitterErrorCode { get; private set; }

        public override string ToString()
        {
            if (TwitterErrorCode.HasValue)
            {
                // HTTP XXX/Twitter YYY: ERROR DESCRIPTION
                return string.Format("HTTP {0}/Twitter {1}: {2}",
                    StatusCode, TwitterErrorCode.Value, Message);
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

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = await base.SendAsync(request, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var rstr = await resp.Content.ReadAsStringAsync();
                var json = DynamicJson.Parse(rstr);
                var ex = new TwitterApiException(resp.StatusCode, rstr);
                try
                {
                    if (json.errors() && json.errors[0].code() && json.errors[0].message())
                    {
                        ex = new TwitterApiException(resp.StatusCode,
                            json.errors[0].message, (int)json.errors[0].code);
                    }
                }
                catch
                {
                    // ignore parse exception 
                }
                throw ex;
            }
            return resp.EnsureSuccessStatusCode();
        }
    }
}
