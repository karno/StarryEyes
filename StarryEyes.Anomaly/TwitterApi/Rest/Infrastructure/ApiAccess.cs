namespace StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure
{
    public class ApiAccess
    {
        private readonly string _path;
        private readonly string _param;

        public ApiAccess(string path, string param = null)
        {
            this._path = path;
            this._param = param;
        }

        public override string ToString()
        {
            return (ApiAccessProperties.ApiEndpoint) +
                   (this._path.StartsWith("/") ? this._path.Substring(1) : this._path) +
                   (string.IsNullOrEmpty(this._param) ? "" : ("?" + this._param));
        }

        public static implicit operator string(ApiAccess access)
        {
            System.Diagnostics.Debug.WriteLine("API Access:" + access);
            return access.ToString();
        }
    }
}