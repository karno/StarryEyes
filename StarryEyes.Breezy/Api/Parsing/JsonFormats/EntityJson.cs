using System.Collections.Generic;
using StarryEyes.Breezy.DataModel;
using System.Linq;

namespace StarryEyes.Breezy.Api.Parsing.JsonFormats
{
    public class EntityJson
    {
        public MediaEntityJson[] media { get; set; }

        public UrlEntityJson[] urls { get; set; }

        public UserMentionsEntityJson[] user_mentions { get; set; }

        public HashtagsEntityJson[] hashtags { get; set; }

        public IEnumerable<TwitterEntity> Spawn()
        {
            return media.Guard()
                .Select(entity => new TwitterEntity(
                    EntityType.Media, entity.display_url, entity.url, entity.media_url,
                    entity.indices[0], entity.indices[1]))
                .Concat(urls.Guard()
                    .Select(entity => new TwitterEntity(
                        EntityType.Urls, entity.display_url, entity.expanded_url,
                        entity.indices[0], entity.indices[1])))
                .Concat(user_mentions.Guard()
                    .Select(entity => new TwitterEntity(
                        EntityType.UserMentions, entity.screen_name, entity.id_str,
                        entity.indices[0], entity.indices[1])))
                .Concat(hashtags.Guard()
                    .Select(entity => new TwitterEntity(
                        EntityType.Hashtags, entity.text, entity.text,
                        entity.indices[0], entity.indices[1])));
        }
    }

    public class MediaEntityJson
    {
        public string id_str { get; set; }

        public string media_url { get; set; }

        public string url { get; set; }

        public string display_url { get; set; }

        public string expanded_url { get; set; }

        // sizes member is not for needed, so this property will be dropped.
        // [DataMember]
        // public TwitterMediaSizesEntityJson[] sizes { get; set; }

        public string type { get; set; }

        public int[] indices { get; set; }
    }

    public class UrlEntityJson
    {
        public string url { get; set; }

        public string display_url { get; set; }

        public string expanded_url { get; set; }

        public int[] indices { get; set; }
    }

    public class UserMentionsEntityJson
    {
        public string id_str { get; set; }

        public string screen_name { get; set; }

        public string name { get; set; }

        public int[] indices { get; set; }
    }

    public class HashtagsEntityJson
    {
        public string text { get; set; }

        public int[] indices { get; set; }
    }
}
