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
        public TwitterApiException(HttpStatusCode httpCode, string message, TwitterErrorCode twitterCode)
            : base(message)
        {
            StatusCode = httpCode;
            TwitterErrorCode = twitterCode;
        }

        public TwitterApiException(HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }

        public TwitterErrorCode? TwitterErrorCode { get; }

        public override string ToString()
        {
            return TwitterErrorCode.HasValue
                ? $"HTTP {StatusCode}/Twitter {TwitterErrorCode.Value}: {Message}"
                : InnerException.ToString();
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
            var resp = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var rstr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = DynamicJson.Parse(rstr);
                var ex = new TwitterApiException(resp.StatusCode, rstr);
                try
                {
                    if (json.errors() && json.errors[0].code() && json.errors[0].message())
                    {
                        ex = new TwitterApiException(resp.StatusCode,
                            json.errors[0].message, (TwitterErrorCode)((int)json.errors[0].code));
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
    public enum TwitterErrorCode
    {
        AuthenticationFailed = 32,
        PageNotExist = 34,
        AccountSuspended = 64,
        ApiNoLongerSupported = 68,
        RateLimitExceeded = 88,
        InvalidOrExpiredToken = 89,
        SslRequired = 92,
        OverCapacity = 130,
        InternalError = 131,
        InvalidSignature = 135,
        TooManyFollow = 161,
        AuthorizationRequired = 179,
        StatusUpdateLimit = 185,
        StatusDuplicated = 187,
        BadAuthenticationData = 215,
        SuspiciousRequest = 226,
        LoginVerificationNeeded = 231,
        EndpointGone = 251,
        ApiPermissionDenined = 261,
        TryToMuteYourself = 271,
        CouldNotMute = 272,
        DirectMessageTooLong = 354,
    }
}
