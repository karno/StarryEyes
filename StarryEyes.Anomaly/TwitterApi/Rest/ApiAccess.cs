namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public class ApiAccess
    {
        private readonly string _path;
        private readonly string _param;

        public ApiAccess(string path, string param = null)
        {
            _path = path;
            _param = param;
        }

        public override string ToString()
        {
            return ApiEndpoint.ApiEndpointPrefix +
                   (_path.StartsWith("/") ? _path.Substring(1) : _path) +
                   (string.IsNullOrEmpty(_param) ? "" : ("?" + _param));
        }

        public static implicit operator string(ApiAccess access)
        {
            return access.ToString();
        }
    }
}