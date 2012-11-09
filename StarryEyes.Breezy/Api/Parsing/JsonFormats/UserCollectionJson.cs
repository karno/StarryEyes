
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class UserCollectionJson
    {
        public UserJson[] users { get; set; }

        public string next_cursor_str { get; set; }

        public string previous_cursor_str { get; set; }
    }
}
