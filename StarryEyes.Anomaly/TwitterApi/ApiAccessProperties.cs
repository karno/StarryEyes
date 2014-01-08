

namespace StarryEyes.Anomaly.TwitterApi
{
    public static class ApiAccessProperties
    {
        private const string DefaultApiEndpoint = "https://api.twitter.com/1.1/";
        private const string DefaultUserAgent = "Krile/StarryEyes (Windows;.NET Framework 4.5) - SarryEyes.Anomaly with AsyncOAuth";
        private const int DefaultStreamTimeoutSec = 90;

        private static string _apiEndpoint;
        private static string _userAgent;
        private static int? _streamTimeoutSec;

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

        /// <summary>
        /// Timeout seconds used in streaming connection.
        /// </summary>
        public static int StreamingTimeoutSec
        {
            get { return _streamTimeoutSec ?? DefaultStreamTimeoutSec; }
            set { _streamTimeoutSec = value == 0 ? (int?)null : value; }
        }
    }
}
