using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class ListJson
    {
        public string created_at { get; set; }

        public int member_count { get; set; }

        public string uri { get; set; }

        public string name { get; set; }

        public string full_name { get; set; }

        public int subscriber_count { get; set; }

        public string description { get; set; }

        public string mode { get; set; }

        public string slug { get; set; }

        public UserJson user { get; set; }

        public string id_str { get; set; }

        public TwitterList Spawn()
        {
            return new TwitterList()
            {
                Id = id_str.ParseLong(),
                CreatedAt = created_at.ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
                Uri = uri.ParseUri(),
                Name = name,
                FullName = full_name,
                SubscriberCount = subscriber_count,
                Description = description,
                ListMode = mode == "public" ? ListMode.Public : ListMode.Private,
                MemberCount = member_count,
                Slug = slug,
                User = user.Spawn()
            };
        }
    }
}
