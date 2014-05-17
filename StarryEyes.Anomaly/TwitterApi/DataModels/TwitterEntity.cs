using System;
using System.Collections.Generic;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterEntity
    {
        public static IEnumerable<TwitterEntity> GetEntities(dynamic json)
        {
            if (json.IsDefined("hashtags"))
            {
                var tags = json.hashtags;
                foreach (var tag in tags)
                {
                    yield return new TwitterEntity
                    {
                        EntityType = EntityType.Hashtags,
                        DisplayText = tag.text,
                        StartIndex = (int)tag.indices[0],
                        EndIndex = (int)tag.indices[1]
                    };
                }
            }
            if (json.IsDefined("media"))
            {
                var medias = json.media;
                foreach (var media in medias)
                {
                    yield return new TwitterEntity
                    {
                        EntityType = EntityType.Media,
                        DisplayText = media.display_url,
                        OriginalUrl = media.url,
                        MediaUrl = media.media_url,
                        StartIndex = (int)media.indices[0],
                        EndIndex = (int)media.indices[1]
                    };
                }
            }
            if (json.IsDefined("urls"))
            {
                var urls = json.urls;
                foreach (var url in urls)
                {
                    string display = url.url;
                    string expanded = url.url;
                    if (url.display_url())
                    {
                        display = url.display_url;
                    }
                    if (url.expanded_url())
                    {
                        expanded = url.expanded_url;
                    }
                    yield return new TwitterEntity
                    {
                        EntityType = EntityType.Urls,
                        DisplayText = display,
                        OriginalUrl = !String.IsNullOrEmpty(expanded) ? expanded : display,
                        StartIndex = (int)url.indices[0],
                        EndIndex = (int)url.indices[1]
                    };
                }
            }
            if (json.IsDefined("user_mentions"))
            {
                var mentions = json.user_mentions;
                foreach (var mention in mentions)
                {
                    yield return new TwitterEntity
                    {
                        EntityType = EntityType.UserMentions,
                        DisplayText = mention.screen_name,
                        UserId = Int64.Parse(mention.id_str),
                        StartIndex = (int)mention.indices[0],
                        EndIndex = (int)mention.indices[1]
                    };
                }
            }
        }

        /// <summary>
        /// Type of this entity.
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// String which represents displaying text.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Unshortened URL of this if this entity describes (shortened) url.
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// Numerical ID of the user if this entity describes user.
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// Url of media. used only for Media entity.
        /// </summary>
        public string MediaUrl { get; set; }

        /// <summary>
        /// Start index of text for attaching entity.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// End index of text for attaching entity.
        /// </summary>
        public int EndIndex { get; set; }
    }

    public enum EntityType
    {
        Media,
        Urls,
        UserMentions,
        Hashtags
    }
}
