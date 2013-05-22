using System.Collections.Generic;
using System.Net.Http;
using AsyncOAuth;
using StarryEyes.Octave.Ext;

namespace StarryEyes.Octave
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
        public static HttpClient CreateOAuthClient(this IOAuthCredential credential,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null)
        {
            return OAuthUtility.CreateOAuthClient(
                credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                optionalHeaders);
        }

        public static HttpClient CreateOAuthEchoClient(this IOAuthCredential credential,
            string serviceProvider, string realm,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null)
        {
            return new HttpClient(
                new OAuthEchoMessageHandler(
                    serviceProvider, realm, credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders));
        }
    }
}
