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
}
