using System;
using System.Collections.Generic;
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
    /// <summary>OAuth Authorization Client</summary>
    public class OAuthAuthorizer : OAuthBase
    {
        public OAuthAuthorizer(string consumerKey, string consumerSecret)
            : base(consumerKey, consumerSecret)
        { }

        private IObservable<TokenResponse<T>> GetTokenResponse<T>(string url, IEnumerable<Parameter> parameters
            , Func<string, string, T> tokenFactory) where T : Token
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Headers[HttpRequestHeader.Authorization] = BuildAuthorizationHeader(parameters);
            req.Method = MethodType.Post.ToUpperString();
            req.ContentType = "application/x-www-form-urlencoded";

            return req.DownloadStringAsync()
                .Select(tokenBase =>
                {
                    var splitted = tokenBase.Split('&').Select(s => s.Split('=')).ToDictionary(s => s.First(), s => s.Last());
                    var token = tokenFactory(splitted["oauth_token"], splitted["oauth_token_secret"]);
                    var extraData = splitted.Where(kvp => kvp.Key != "oauth_token" && kvp.Key != "oauth_token_secret")
                        .ToLookup(kvp => kvp.Key, kvp => kvp.Value);
                    return new TokenResponse<T>(token, extraData);
                });
        }

        /// <summary>construct AuthrizeUrl + RequestTokenKey</summary>
        public string BuildAuthorizeUrl(string authUrl, RequestToken requestToken)
        {
            Guard.ArgumentNull(authUrl, "authUrl");
            Guard.ArgumentNull(requestToken, "accessToken");

            return authUrl + "?oauth_token=" + requestToken.Key;
        }

        /// <summary>asynchronus get RequestToken</summary>
        /// <param name="otherParameters">need parameters except consumer_key,timestamp,nonce,signature,signature_method,version</param>
        public IObservable<TokenResponse<RequestToken>> GetRequestToken(string requestTokenUrl, params Parameter[] otherParameters)
        {
            return GetRequestToken(requestTokenUrl, otherParameters.AsEnumerable());
        }

        /// <summary>asynchronus get RequestToken</summary>
        /// <param name="otherParameters">need parameters except consumer_key,timestamp,nonce,signature,signature_method,version</param>
        public IObservable<TokenResponse<RequestToken>> GetRequestToken(string requestTokenUrl, IEnumerable<Parameter> otherParameters)
        {
            Guard.ArgumentNull(requestTokenUrl, "requestTokenUrl");
            Guard.ArgumentNull(otherParameters, "otherParameters");

            var parameters = ConstructBasicParameters(requestTokenUrl, MethodType.Post, null, otherParameters);
            parameters.Add(otherParameters);
            return GetTokenResponse(requestTokenUrl, parameters, (key, secret) => new RequestToken(key, secret));
        }

        /// <summary>asynchronus get GetAccessToken</summary>
        public IObservable<TokenResponse<AccessToken>> GetAccessToken(string accessTokenUrl, RequestToken requestToken, string verifier)
        {
            Guard.ArgumentNull(accessTokenUrl, "accessTokenUrl");
            Guard.ArgumentNull(requestToken, "requestToken");
            Guard.ArgumentNull(verifier, "verifier");

            var verifierParam = new Parameter("oauth_verifier", verifier);
            var parameters = ConstructBasicParameters(accessTokenUrl, MethodType.Post, requestToken, verifierParam);
            parameters.Add(verifierParam);
            return GetTokenResponse(accessTokenUrl, parameters, (key, secret) => new AccessToken(key, secret));
        }

        /// <summary>asynchronus get GetAccessToken for xAuth</summary>
        public IObservable<TokenResponse<AccessToken>> GetAccessToken(string accessTokenUrl, string xauthUserName, string xauthPassword, string xauthMode = "client_auth")
        {
            Guard.ArgumentNull(accessTokenUrl, "accessTokenUrl");
            Guard.ArgumentNull(xauthUserName, "xauthUserName");
            Guard.ArgumentNull(xauthPassword, "xauthPassword");
            Guard.ArgumentNull(xauthMode, "xauthMode");

            var xauthParams = new ParameterCollection
            {
                {"x_auth_username", xauthUserName},
                {"x_auth_password", xauthPassword},
                {"x_auth_mode", xauthMode}
            };
            var parameters = ConstructBasicParameters(accessTokenUrl, MethodType.Post, null, xauthParams);

            return GetTokenResponse(accessTokenUrl + "?" + xauthParams.ToQueryParameter(),
                parameters, (key, secret) => new AccessToken(key, secret));
        }
    }
}