

namespace StarryEyes.Anomaly.TwitterApi
{
    public static class ApiAccessProperties
    {
        static ApiAccessProperties()
        {
            UserAgent = "Krile/3.x (Windows;.NET Framework 4.5);SarryEyes.Anomaly;AsyncOAuth";
            ApiEndpoint = "https://api.twitter.com/1.1/";
        }

        /// <summary>
        /// Api endpoint for accessing twitter.
        /// </summary>
        public static string ApiEndpoint { get; set; }

        /// <summary>
        /// User agent for accessing twitter.
        /// </summary>
        public static string UserAgent { get; set; }
    }
}
