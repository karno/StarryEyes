
namespace StarryEyes.Anomaly.TwitterApi
{
    /// <summary>
    /// Describe basic properties for accessing API.
    /// </summary>
    public interface IApiAccessProperties
    {
        string Endpoint { get; }

        string UserAgent { get; }

        bool UseGZip { get; }
    }

    /// <summary>
    /// Provide default API access propeties.
    /// </summary>
    public static class ApiAccessProperties
    {
        private const string DefaultEndpoint = "https://api.twitter.com/1.1/";
        private const string DefaultEndpointForUpload = "https://upload.twitter.com/1.1/";
        private const string DefaultEndpointForUserStreams = "https://userstream.twitter.com/1.1/";

        private const string DefaultUserAgent = "StarryEyes.Anomaly/2.0 (Krile/3.0;StarryEyes Illumine)";

        private static IApiAccessProperties _default;

        public static IApiAccessProperties Default
        {
            get { return _default ?? (_default = new DefaultApiAccessProperties(DefaultEndpoint)); }
        }

        private static IApiAccessProperties _defaultForUpload;

        public static IApiAccessProperties DefaultForUpload
        {
            get
            {
                return _defaultForUpload ??
                       (_defaultForUpload = new DefaultApiAccessProperties(DefaultEndpointForUpload));
            }
        }

        private static IApiAccessProperties _defaultForUserStreams;

        public static IApiAccessProperties DefaultForUserStreams
        {
            get
            {
                return _defaultForUserStreams ??
                       (_defaultForUserStreams = new DefaultApiAccessProperties(DefaultEndpointForUserStreams, false));
            }
        }

        private sealed class DefaultApiAccessProperties : IApiAccessProperties
        {
            private readonly string _endpoint;
            private readonly bool _useGZip;

            public DefaultApiAccessProperties(string endpoint, bool useGZip = true)
            {
                _endpoint = endpoint;
                _useGZip = useGZip;
            }

            public string Endpoint
            {
                get { return _endpoint; }
            }

            public string UserAgent
            {
                get { return DefaultUserAgent; }
            }

            public bool UseGZip
            {
                get { return _useGZip; }
            }
        }
    }
}
