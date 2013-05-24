using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncOAuth;

namespace StarryEyes.Anomaly.Ext
{
    public class OAuthEchoMessageHandler : DelegatingHandler
    {
        private const string OAuthEchoAuthorizationHeader = "X-Verify-Credentials-Authorization";
        private const string OAuthEchoServiceProvider = "X-Auth-Service-Provider";

        public OAuthEchoMessageHandler(string serviceProvider, string realm, string consumerKey, string consumerSecret,
                                        Token token = null,
                                        IEnumerable<KeyValuePair<string, string>> optionalOAuthHeaderParameters = null)
            : this(new HttpClientHandler(), serviceProvider, realm, consumerKey, consumerSecret, token,
                optionalOAuthHeaderParameters)
        {
        }

        public OAuthEchoMessageHandler(HttpMessageHandler innerHandler, string serviceProvider, string realm,
                                        string consumerKey, string consumerSecret,
                                        Token token = null,
                                        IEnumerable<KeyValuePair<string, string>> optionalOAuthHeaderParameters = null)
            : base(new OAuthMessageHandler(new OAuthEchoMessagePostHandler(innerHandler, serviceProvider),
                                           consumerKey, consumerSecret, token, AddRealm(optionalOAuthHeaderParameters, realm)))
        {
        }

        private static IEnumerable<KeyValuePair<string, string>> AddRealm(IEnumerable<KeyValuePair<string, string>> param, string realm)
        {
            var rp = new[] { new KeyValuePair<string, string>("Realm", realm) };
            return param == null ? rp : param.Concat(rp);
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private class OAuthEchoMessagePostHandler : DelegatingHandler
        {
            private readonly string _serviceProvider;

            public OAuthEchoMessagePostHandler(HttpMessageHandler innerHandler, string serviceProvider)
                : base(innerHandler)
            {
                _serviceProvider = serviceProvider;
            }

            protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Headers.Add(OAuthEchoAuthorizationHeader,
                                    request.Headers.Authorization.Scheme + " " + request.Headers.Authorization.Parameter);
                request.Headers.Add(OAuthEchoServiceProvider, _serviceProvider);
                request.Headers.Authorization = null;
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
