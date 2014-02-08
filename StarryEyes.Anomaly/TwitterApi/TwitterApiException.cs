using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;

namespace StarryEyes.Anomaly.TwitterApi
{
    public class TwitterApiException : Exception
    {
        public TwitterApiException(HttpStatusCode statusCode, string message, int code)
            : base(message)
        {
            this.StatusCode = statusCode;
            this.TwitterErrorCode = code;
        }

        public TwitterApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            this.StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public int? TwitterErrorCode { get; private set; }

        public override string ToString()
        {
            if (this.TwitterErrorCode.HasValue)
            {
                return this.TwitterErrorCode.Value + ": " + Message;
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
            var resp = await base.SendAsync(request, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var rstr = await resp.Content.ReadAsStringAsync();
                var json = DynamicJson.Parse(rstr);
                var ex = new TwitterApiException(resp.StatusCode, rstr);
                try
                {
                    var eflg = json.errors();
                    var cflg = json.errors[0].code();
                    var mflg = json.errors[0].message();
                    if (json.errors() && json.errors[0].code() && json.errors[0].message())
                    {
                        ex = new TwitterApiException(resp.StatusCode,
                                                      json.errors[0].message, (int)json.errors[0].code);
                    }
                }
                catch { }
                throw ex;
            }
            return resp.EnsureSuccessStatusCode();
        }
    }
}
