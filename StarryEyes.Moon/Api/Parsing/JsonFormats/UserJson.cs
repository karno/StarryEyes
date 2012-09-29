using Newtonsoft.Json;
using StarryEyes.Moon.DataModel;
using StarryEyes.Moon.Util;

namespace StarryEyes.Moon.Api.Parsing.JsonFormats
{
    public class UserJson
    {
        public string id_str { get; set; }

        public string screen_name { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string location { get; set; }

        public string url { get; set; }

        [JsonProperty("protected")]
        public bool is_protected { get; set; }

        public bool is_translator { get; set; }

        [JsonProperty("verified")]
        public bool is_verified { get; set; }

        [JsonProperty("contributors_enabled")]
        public bool is_contributors_enabled { get; set; }

        public string profile_image_url { get; set; }

        public int statuses_count { get; set; }

        public int friends_count { get; set; }

        public int followers_count { get; set; }

        public int favourites_count { get; set; }

        public int listed_count { get; set; }

        public string lang { get; set; }

        [JsonProperty("geo_enabled")]
        public bool is_geo_enabled { get; set; }

        public string created_at { get; set; }

        /// <summary>
        /// Convert JSON internal object model to Twintail object model.
        /// </summary>
        /// <returns></returns>
        public TwitterUser Spawn()
        {
            var profimg = profile_image_url.ParseUri();
            if (profimg != null && !profimg.IsAbsoluteUri)
            {
                // Twitter sometimes returns partial url.
                profimg = null;
            }
            return new TwitterUser()
            {
                Id = id_str.ParseLong(),
                ScreenName = screen_name,
                Name = name,
                Description = description,
                Location = location,
                Url = url,
                IsProtected = is_protected,
                IsTranslator = is_translator,
                IsVerified = is_verified,
                IsContributorsEnabled = is_contributors_enabled,
                ProfileImageUri = profimg,
                StatusesCount = (long)statuses_count,
                FriendsCount = (long)friends_count,
                FollowersCount = (long)followers_count,
                FavoritesCount = (long)favourites_count,
                ListedCount = (long)listed_count,
                Language = lang,
                IsGeoEnabled = is_geo_enabled,
                CreatedAt = created_at.ParseDateTime(XmlParser.TwitterDateTimeFormat)
            };
        }
    }
}
