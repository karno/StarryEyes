

namespace StarryEyes.Anomaly.TwitterApi
{
    public static class ApiEndpoint
    {
        private static string _userAgent = "StarryEyes.Anomaly/AsyncOAuth";
        /// <summary>
        /// default user agent of connection.
        /// </summary>
        public static string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        public static readonly string ApiEndpointPrefix = "https://api.twitter.com/1.1/";

        public static string ApiEndpointProxy { get; set; }
    }
}
