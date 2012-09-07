using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

#if WINDOWS_PHONE
using Microsoft.Phone.Reactive;
#else
using System.Reactive;
using System.Reactive.Linq;
#endif

namespace Codeplex.OAuth
{
    /// <summary>access protected resource client</summary>
    public class OAuthClient : OAuthBase
    {
        public AccessToken AccessToken { get; private set; }
        public ParameterCollection Parameters { get; set; }
        public string Url { get; set; }
        public string Realm { get; set; }
        public MethodType MethodType { get; set; }
        public Action<HttpWebRequest> ApplyBeforeRequest { get; set; }

        public OAuthClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
            : this(consumerKey, consumerSecret, new AccessToken(accessToken, accessTokenSecret))
        { }

        public OAuthClient(string consumerKey, string consumerSecret, AccessToken accessToken)
            : base(consumerKey, consumerSecret)
        {
            Guard.ArgumentNull(accessToken, "accessToken");

            this.AccessToken = accessToken;
            Parameters = new ParameterCollection();
            MethodType = OAuth.MethodType.Get;
        }

        protected string AuthorizationHeader
        {
            get
            {
                var realm = (Realm != null) ? new[] { new Parameter("realm", Realm) } : Enumerable.Empty<Parameter>();
                var parameters = ConstructBasicParameters(Url, MethodType, AccessToken, Parameters);
                return BuildAuthorizationHeader(realm.Concat(parameters));
            }
        }

        protected virtual WebRequest CreateWebRequest()
        {
            var requestUrl = (MethodType == OAuth.MethodType.Get) ? Url + "?" + Parameters.ToQueryParameter() : Url;
            var req = (HttpWebRequest)WebRequest.Create(requestUrl);
#if WINDOWS_PHONE
            req.AllowReadStreamBuffering = false;
#endif
            req.Headers[HttpRequestHeader.Authorization] = this.AuthorizationHeader;
            req.Method = MethodType.ToUpperString();
            if (MethodType == OAuth.MethodType.Post) req.ContentType = "application/x-www-form-urlencoded";
            if (ApplyBeforeRequest != null) ApplyBeforeRequest(req);

            return req;
        }

        /// <summary>asynchronus GetResponse</summary>
        public IObservable<WebResponse> GetResponse()
        {
            if (Url == null) throw new InvalidOperationException("must set Url before call");

            var req = CreateWebRequest();
            switch (MethodType)
            {
                case MethodType.Get:
                    return Observable.Defer(() => req.GetResponseAsObservable());
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