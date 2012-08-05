using StarryEyes.SweetLady.DataModel;
using StarryEyes.SweetLady.Util;
using System.Linq;

namespace StarryEyes.SweetLady.Api.Parsing.JsonFormats
{
    public class TweetJson
    {
        public string id_str { get; set; }

        public string source { get; set; }

        public string text { get; set; }

        public string created_at { get; set; }

        public bool favorited { get; set; }

        public string in_reply_to_status_id_str { get; set; }

        public string in_reply_to_user_id_str { get; set; }

        public UserJson user { get; set; }

        public string in_reply_to_screen_name { get; set; }

        public CoordinatesJson coordinates { get; set; }

        public TweetJson retweeted_status { get; set; }

        // geo is deprecated parameter.
        // public int[] geo { get; set; }

        public EntityJson entities { get; set; }

        /// <summary>
        /// Create actual TwitterStatus.
        /// </summary>
        public TwitterStatus Spawn()
        {
            return new TwitterStatus()
            {
                StatusType = StatusType.Tweet,
                Id = id_str.ParseLong(),
                IsDataLacking = false,
                Source = source,
                Text = text,
                IsFavored = favorited,
                CreatedAt = created_at.ParseDateTime(XmlParser.TwitterDateTimeFormat),
                InReplyToStatusId = in_reply_to_status_id_str == null || in_reply_to_status_id_str == "0" ? null :
                    (long?)in_reply_to_status_id_str.ParseLong(),
                InReplyToUserId = in_reply_to_user_id_str == null || in_reply_to_user_id_str == "0" ? null :
                    (long?)in_reply_to_user_id_str.ParseLong(),
                User = user.Spawn(),
                InReplyToScreenName = in_reply_to_screen_name,
                Longitude = coordinates == null ? null : (double?)coordinates.coordinates[0],
                Latitude = coordinates == null ? null : (double?)coordinates.coordinates[1],
                RetweetedOriginal = retweeted_status == null ? null : retweeted_status.Spawn(),
                RetweetedOriginalId = retweeted_status == null ? null : (long?)retweeted_status.id_str.ParseLong(),
                Entities = entities != null ? entities.Spawn().ToArray() : new TwitterEntity[0]
                // contributors?
            };
        }
    }
}
