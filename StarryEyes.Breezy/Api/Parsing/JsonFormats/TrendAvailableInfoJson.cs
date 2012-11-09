
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class TrendAvailableInfoJson
    {
        public string name { get; set; }

        public int woeid { get; set; }

        public string country { get; set; }

        public string url { get; set; }

        public string countryCode { get; set; }
    }

    public class TrendPlaceTypeJson
    {
        public string name { get; set; }

        public int code { get; set; }
    }
}
