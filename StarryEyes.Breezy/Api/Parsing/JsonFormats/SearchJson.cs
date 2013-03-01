
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class SearchJson
    {
        public TweetJson[] statuses { get; set; }

        public SearchMetadataJson search_metadata { get; set; }
    }

    public class SearchMetadataJson
    {
        public string max_id_str { get; set; }

        public int since_id_str { get; set; }

        public string next_results { get; set; }

        public string query { get; set; }
    }
}