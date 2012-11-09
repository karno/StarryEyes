
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class ListCollectionsJson
    {
        public ListJson[] lists { get; set; }

        public string next_cursor_str { get; set; }

        public string previous_cursor_str { get; set; }
    }
}
