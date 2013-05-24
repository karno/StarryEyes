using System.Collections.Generic;
using System.Net.Http;
using AsyncOAuth;
using StarryEyes.Anomaly.Ext;

namespace StarryEyes.Anomaly
{
    public interface IOAuthCredential
    {
        string OAuthConsumerKey { get; }

        string OAuthConsumerSecret { get; }

        string OAuthAccessToken { get; }

        string OAuthAccessTokenSecret { get; }
    }

    public static class OAuthCredentialExtension
    {
        private static string GetConsumerKey(this IOAuthCredential credential)
        {
            return credential.OAuthConsumerKey ?? Core.DefaultConsumerKey;
        }

        private static string GetConsumerSecret(this IOAuthCredential credential)
        {
            return credential.OAuthConsumerSecret ?? Core.DefaultConsumerSecret;
        }

        public static HttpClient CreateOAuthClient(this IOAuthCredential credential,
                IEnumerable<KeyValuePair<string, string>> optionalHeaders = null)
        {
            return OAuthUtility.CreateOAuthClient(
                credential.GetConsumerKey(), credential.GetConsumerSecret(),
                new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                optionalHeaders);
        }

        public static HttpClient CreateOAuthEchoClient(this IOAuthCredential credential,
            string serviceProvider, string realm,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null)
        {
            return new HttpClient(
                new OAuthEchoMessageHandler(
                    serviceProvider, realm,
                    credential.GetConsumerKey(), credential.GetConsumerSecret(),
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders));
        }
    }
}
