using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.Ext
{
    class BearerMessageHandler : DelegatingHandler
    {
        private readonly string _bearerToken;

        public BearerMessageHandler(string bearerToken)
            : this(new HttpClientHandler(), bearerToken)
        {

        }

        public BearerMessageHandler(HttpMessageHandler innerHandler, string bearerToken)
            : base(innerHandler)
        {
            _bearerToken = bearerToken;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Bearer", _bearerToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
