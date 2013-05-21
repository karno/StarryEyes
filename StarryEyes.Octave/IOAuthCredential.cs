using System.Collections.Generic;
using System.Net.Http;
using AsyncOAuth;

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
        public static HttpClient CreateOAuthClient(this IOAuthCredential credential, IEnumerable<KeyValuePair<string, string>> optionalHeaders = null)
        {
            return OAuthUtility.CreateOAuthClient(
                credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                optionalHeaders);
        }
    }
}
