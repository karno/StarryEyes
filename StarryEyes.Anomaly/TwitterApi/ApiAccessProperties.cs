

namespace StarryEyes.Anomaly.TwitterApi
{
    public static class ApiAccessProperties
    {
        private const string DefaultApiEndpoint = "https://api.twitter.com/1.1/";
        private const string DefaultUserAgent = "Krile/3.x (Windows;.NET Framework 4.5);SarryEyes.Anomaly;AsyncOAuth";

        private static string _apiEndpoint;
        private static string _userAgent;

        /// <summary>
        /// Api endpoint for accessing twitter.
        /// </summary>
        public static string ApiEndpoint
        {
            get { return _apiEndpoint ?? DefaultApiEndpoint; }
            set { _apiEndpoint = !string.IsNullOrEmpty(value) ? value : null; }
        }

        /// <summary>
        /// User agent for accessing twitter.
        /// </summary>
        public static string UserAgent
        {
            get { return _userAgent ?? DefaultUserAgent; }
            set { _userAgent = !string.IsNullOrEmpty(value) ? value : null; }
        }
    }
}
