using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using AsyncOAuth;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi;

namespace StarryEyes.Anomaly
{
    public static class OAuthCredentialExtension
    {
        private const string UserAgentHeader = "User-Agent";

        public static HttpClient CreateOAuthClient(
            this IOAuthCredential credential,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null,
            bool useGZip = true)
        {
            return new HttpClient(
                new OAuthMessageHandler(
                    GetInnerHandler(useGZip),
                    credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders))
                .SetUserAgent(ApiAccessProperties.UserAgent);
        }

        public static HttpClient CreateOAuthEchoClient(
            this IOAuthCredential credential,
            string serviceProvider, string realm,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null,
            bool useGZip = true)
        {
            return new HttpClient(
                new OAuthEchoMessageHandler(
                    GetInnerHandler(useGZip),
                    serviceProvider, realm, credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders))
                    .SetUserAgent(ApiAccessProperties.UserAgent);
        }

        public static HttpClient SetUserAgent(this HttpClient client, string userAgent)
        {
            // remove before add user agent
            client.DefaultRequestHeaders.Remove(UserAgentHeader);
            client.DefaultRequestHeaders.Add(UserAgentHeader, userAgent);
            return client;
        }

        private static HttpMessageHandler GetInnerHandler(bool useGZip)
        {
            var proxy = Core.GetWebProxy();
            return new TwitterApiExceptionHandler(
                new HttpClientHandler
                {
                    AutomaticDecompression =
                        useGZip
                            ? DecompressionMethods.GZip | DecompressionMethods.Deflate
                            : DecompressionMethods.None,
                    Proxy = proxy,
                    UseProxy = proxy != null
                });
        }

        internal static FormUrlEncodedContent ParametalizeForPost(this Dictionary<string, object> dict)
        {
            return new FormUrlEncodedContent(
                dict.Where(kvp => kvp.Value != null)
                    .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString())));
        }

        internal static string ParametalizeForGet(this Dictionary<string, object> dict)
        {
            return dict.Where(kvp => kvp.Value != null)
                       .OrderBy(kvp => kvp.Key)
                       .Select(kvp => kvp.Key +
                                      "=" +
                                      EncodeForParameters(kvp.Value.ToString()))
                       .JoinString("&");
        }

        const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        private static string EncodeForParameters(string value)
        {
            var result = new StringBuilder();
            var data = Encoding.UTF8.GetBytes(value);
            var len = data.Length;

            for (var i = 0; i < len; i++)
            {
                int c = data[i];
                if (c < 0x80 && AllowedChars.IndexOf((char)c) != -1)
                {
                    result.Append((char)c);
                }
                else
                {
                    result.Append('%' + String.Format("{0:x2}", (int)data[i]));
                }
            }
            return result.ToString();
        }
    }
}
