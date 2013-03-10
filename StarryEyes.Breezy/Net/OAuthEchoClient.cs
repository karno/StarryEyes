using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using Codeplex.OAuth;

namespace StarryEyes.Breezy.Net
{
    public class OAuthEchoClient : OAuthBase
    {
        public AccessToken AccessToken { get; private set; }
        public ParameterCollection Parameters { get; set; }
        public string Url { get; set; }
        public string Realm { get; set; }
        public MethodType MethodType { get; set; }
        public Action<HttpWebRequest> ApplyBeforeRequest { get; set; }

        public OAuthEchoClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
            : this(new AccessToken(accessToken, accessTokenSecret), consumerKey, consumerSecret)
        { }

        public OAuthEchoClient(AccessToken token, string consumerKey, string consumerSecret)
            : base(consumerKey, consumerSecret)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            this.AccessToken = token;
            Parameters = new ParameterCollection();
            MethodType = MethodType.Get;
        }

        private WebRequest CreateWebRequest(string url)
        {
            const string ServiceProvider = "https://api.twitter.com/1.1/account/verify_credentials.json";
            const string Realm = "http://api.twitter.com/";

            var req = WebRequest.Create(url) as HttpWebRequest;

            // generate oauth signature and parameters
            var parameters = ConstructBasicParameters(ServiceProvider, MethodType.Get, AccessToken);
            // make auth header string
            var authHeader = BuildAuthorizationHeader(new[] { new Parameter("Realm", Realm) }.Concat(parameters));

            // set authenticate headers
            req.Headers["X-Verify-Credentials-Authorization"] = authHeader;
            req.Headers["X-Auth-Service-Provider"] = ServiceProvider;

            req.Method = MethodType.ToString();

            ApplyBeforeRequest(req);
            return req;
        }

        /// <summary>asynchronus GetResponse</summary>
        public IObservable<WebResponse> GetResponse()
        {
            if (Url == null) throw new InvalidOperationException("must set Url before call");

            var req = CreateWebRequest(Url);
            switch (MethodType)
            {
                case MethodType.Get:
                    return Observable.Defer(req.GetResponseAsObservable);
                case MethodType.Post:
                    var postData = Encoding.UTF8.GetBytes(Parameters.ToQueryParameter());
                    return req.UploadDataAsync(postData);
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>asynchronus GetResponse and return ResponseText</summary>
        public IObservable<string> GetResponseText()
        {
            return GetResponse().SelectMany(res => res.DownloadStringAsync());
        }

        /// <summary>asynchronus GetResponse and return onelines</summary>
        public IObservable<string> GetResponseLines()
        {
            return GetResponse().SelectMany(res => res.DownloadStringLineAsync());
        }
    }
}
