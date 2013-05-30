namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public class ApiAccess
    {
        private readonly string _path;

        public ApiAccess(string path)
        {
            _path = path;
        }

        public override string ToString()
        {
            return ApiEndpoint.ApiEndpointPrefix + (_path.StartsWith("/") ? _path.Substring(1) : _path);
        }

        public static implicit operator string(ApiAccess access)
        {
            return access.ToString();
        }
    }
}