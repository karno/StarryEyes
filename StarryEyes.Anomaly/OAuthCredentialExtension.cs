using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using AsyncOAuth;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly
{
    public static class OAuthCredentialExtension
    {
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
                    optionalHeaders));
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
                    optionalHeaders));
        }

        private static HttpMessageHandler GetInnerHandler(bool useGZip)
        {
            if (useGZip)
            {
                return new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
            }
            return new HttpClientHandler();
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
                       .Select(kvp => HttpUtility.UrlEncode(kvp.Key) +
                                      "=" +
                                      HttpUtility.UrlEncode(kvp.Value.ToString()))
                       .JoinString("&");
        }
    }
}
