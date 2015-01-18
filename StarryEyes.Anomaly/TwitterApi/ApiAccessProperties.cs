

using System;

namespace StarryEyes.Anomaly.TwitterApi
{
    public static class ApiAccessProperties
    {
        private const string DefaultApiEndpoint = "https://api.twitter.com/1.1/";
        private const string DefaultUploadEndpoint = "https://upload.twitter.com/1.1/";
        private const string DefaultUserStreamsEndpoint = "https://userstream.twitter.com/1.1/";

        private const string DefaultUserAgent =
            "Krile/StarryEyes (Windows;.NET Framework 4.5) - SarryEyes.Anomaly with AsyncOAuth";

        private const int DefaultStreamTimeoutSec = 90;

        private static string _apiEndpoint = DefaultApiEndpoint;
        private static string _uploadEndpoint = DefaultUploadEndpoint;
        private static string _userStreamsEndpoint = DefaultUserStreamsEndpoint;
        private static string _userAgent;
        private static int? _streamTimeoutSec;

        /// <summary>
        /// API endpoint for accessing twitter.
        /// if set this property as null or empty, reset to default endpoint.
        /// </summary>
        public static string ApiEndpoint
        {
            get { return _apiEndpoint; }
            set { _apiEndpoint = !string.IsNullOrEmpty(value) ? value : DefaultApiEndpoint; }
        }

        /// <summary>
        /// API endpoint for uploading medias.
        /// if set this property as null or empty, reset to default endpoint.
        /// </summary>
        public static string UploadEndpoint
        {
            get { return _uploadEndpoint; }
            set { _uploadEndpoint = !String.IsNullOrEmpty(value) ? value : DefaultUploadEndpoint; }
        }

        /// <summary>
        /// API endpoint for receiving user-streams.
        /// if set this property as null or empty, reset to default endpoint.
        /// </summary>
        public static string UserStreamsEndpoint
        {
            get { return _userStreamsEndpoint; }
            set { _userStreamsEndpoint = !String.IsNullOrEmpty(value) ? value : DefaultUserStreamsEndpoint; }
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
