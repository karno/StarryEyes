using System.Collections.Generic;
using System.Linq;
using System.Net;
using Codeplex.OAuth;
using StarryEyes.Breezy.Authorize;

namespace StarryEyes.Breezy.Api
{
    public static class ApiEndpoint
    {
        private static string _userAgent = "StarryEyes.Breezy with ReactiveOAuth";
        /// <summary>
        /// default user agent of connection.
        /// </summary>
        public static string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        public static readonly string EndpointApiV1a = "https://api.twitter.com/1.1/";

        public static string JoinUrl(this string endpoint, string url)
        {
            return endpoint + (url.StartsWith("/") ? url.Substring(1) : url);
        }

        /// <summary>
        /// Set consumer key.
        /// </summary>
        public static string DefaultConsumerKey { get; set; }

        /// <summary>
        /// Set consumer secret.
        /// </summary>
        public static string DefaultConsumerSecret { get; set; }

        /// <summary>
        /// Get OAuth client.
        /// </summary>
        /// <param name="info">authentication information</param>
        /// <param name="useGzip">flag of GZip enabled</param>
        /// <returns></returns>
        internal static OAuthClient GetOAuthClient(this AuthenticateInfo info, bool useGzip = true)
        {
            var client = info.AccessToken.GetOAuthClient(
                info.OverridedConsumerKey, info.OverridedConsumerSecret);
            return useGzip ? client.UseGZip() : client;
        }

        internal static OAuthClient GetOAuthClient(this AccessToken token,
            string overrideConsumerKey, string overrideConsumerSecret)
        {
            return new OAuthClient(overrideConsumerKey ?? DefaultConsumerKey,
                overrideConsumerSecret ?? DefaultConsumerSecret,
                token)
            {
                ApplyBeforeRequest = req =>
                {
                    req.UserAgent = UserAgent;
                }
            };
        }

        internal static OAuthClient SetEndpoint(this OAuthClient client, string url)
        {
            client.Url = url;
            return client;
        }

        internal static OAuthClient SetParameters(this OAuthClient client, ParameterCollection collection)
        {
            client.Parameters = collection;
            return client;
        }

        internal static OAuthClient SetMethodType(this OAuthClient client, MethodType methodType)
        {
            client.MethodType = methodType;
            return client;
        }

        internal static OAuthClient UseGZip(this OAuthClient client)
        {
            client.ApplyBeforeRequest = req => req.AutomaticDecompression = DecompressionMethods.GZip;
            return client;
        }

        /// <summary>
        /// Build parameters from dictionary.
        /// </summary>
        internal static ParameterCollection Parametalize(this Dictionary<string, object> dict)
        {
            var ret = new ParameterCollection();
            dict.Keys.Select(key => new { key, value = dict[key] })
                .Where(t => t.value != null)
                .ForEach(t => ret.Add(new Parameter(t.key, t.value)));
            return ret;
        }
    }
}
