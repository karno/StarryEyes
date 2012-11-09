
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class TrendInfoJson
    {
        public string created_at { get; set; }

        public TrendItemJson[] trends { get; set; }

        public string as_of { get; set; }

        public TrendLocationJson[] locations { get; set; }
    }

    public class TrendItemJson
    {
        public string name { get; set; }

        public string url { get; set; }

        public string query { get; set; }
    }

    public class TrendLocationJson
    {
        public string name { get; set; }

        public int woeid { get; set; }
    }
}
