
namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class FriendshipJson
    {
        public FriendshipInfoJson relationship { get; set; }
    }

    public class FriendshipInfoJson
    {
        public FriendshipTargetJson target { get; set; }

        public FriendshipSourceJson source { get; set; }
    }

    public class FriendshipTargetJson
    {
        public string id_str { get; set; }

        public string screen_name { get; set; }

        public bool following { get; set; }

        public bool followed_by { get; set; }

    }

    public class FriendshipSourceJson
    {
        public string id_str { get; set; }

        public string screen_name { get; set; }

        public bool following { get; set; }

        public bool followed_by { get; set; }

        public bool notifications_enabled { get; set; }

        public bool can_dm { get; set; }

        public bool want_retweets { get; set; }

        public bool marked_spam { get; set; }

        public bool all_replies { get; set; }

        public bool blocking { get; set; }
    }
}
