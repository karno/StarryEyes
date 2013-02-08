using Newtonsoft.Json;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
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

        [JsonProperty("default_profile_image")]
        public bool is_default_profile_image { get; set; }

        public string profile_image_url { get; set; }

        public string profile_image_url_https { get; set; }

        public string profile_background_image_url { get; set; }

        public string profile_background_image_url_https { get; set; }

        public int statuses_count { get; set; }

        public int friends_count { get; set; }

        public int followers_count { get; set; }

        public int favourites_count { get; set; }

        public int? listed_count { get; set; }

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
            return new TwitterUser
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
                ProfileImageUri = profile_image_url.ParseUriAbsolute(),
                ProfileImageUriHttps = profile_image_url_https.ParseUriAbsolute(),
                ProfileBackgroundImageUri = profile_background_image_url.ParseUriAbsolute(),
                ProfileBackgroundImageUriHttps = profile_background_image_url_https.ParseUriAbsolute(),
                IsDefaultProfileImage = is_default_profile_image,
                StatusesCount = statuses_count,
                FriendsCount = friends_count,
                FollowersCount = followers_count,
                FavoritesCount = favourites_count,
                ListedCount = listed_count.GetValueOrDefault(),
                Language = lang,
                IsGeoEnabled = is_geo_enabled,
                CreatedAt = created_at.ParseDateTime(XmlParser.TwitterDateTimeFormat)
            };
        }
    }
}
