using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class SearchTweetJson
    {
        public string created_at { get; set; }

        public string from_user { get; set; }

        public string from_user_id_str { get; set; }

        public string from_user_name { get; set; }

        public string to_user_id_str { get; set; }

        public string id_str { get; set; }

        public string profile_image_url { get; set; }

        public string source { get; set; }

        public string text { get; set; }

        public EntityJson entities { get; set; }

        public TwitterStatus Spawn()
        {
            return new TwitterStatus()
            {
                StatusType = StatusType.Tweet,
                IsDataLacking = true,
                Id = id_str.ParseLong(),
                Source = XmlParser.ResolveEntity(source),
                Text = text,
                IsFavored = false,
                CreatedAt = created_at.ParseDateTime(), // Can parse with default format rule
                InReplyToUserId = to_user_id_str == null || to_user_id_str == "0" ? null :
                    (long?)to_user_id_str.ParseLong(),
                User = new TwitterUser()
                {
                    IsDataLacking = true,
                    Id = from_user_id_str.ParseLong(),
                    ScreenName = from_user,
                    Name = from_user_name,
                    ProfileImageUri = profile_image_url.ParseUri(),
                },
            };
        }
    }
}
