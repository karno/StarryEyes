
namespace StarryEyes.SweetLady.Api.Parsing.JsonFormats
{
    public class SearchJson
    {
        public string max_id_str { get; set; }

        public string next_page { get; set; }

        public int page { get; set; }

        public string query { get; set; }

        public SearchTweetJson[] results { get; set; }

        public int results_per_page { get; set; }

        public int since_id_str { get; set; }
    }
}
