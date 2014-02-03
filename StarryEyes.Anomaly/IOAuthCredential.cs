namespace StarryEyes.Anomaly
{
    public interface IOAuthCredential
    {
        long Id { get; }

        string OAuthConsumerKey { get; }

        string OAuthConsumerSecret { get; }

        string OAuthAccessToken { get; }

        string OAuthAccessTokenSecret { get; }
    }
}
