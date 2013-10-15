using System;
using System.Collections.Generic;
using System.IO;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterEntity : IBinarySerializable
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
        /// String which represents displaying text. <para />
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
        /// Start index of this element
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// End index of this element
        /// </summary>
        public int EndIndex { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)EntityType);
            writer.Write(DisplayText ?? string.Empty);
            writer.Write(OriginalUrl ?? string.Empty);
            writer.Write(UserId);
            writer.Write(MediaUrl != null);
            if (MediaUrl != null)
                writer.Write(MediaUrl);
            writer.Write(StartIndex);
            writer.Write(EndIndex);
        }

        public void Deserialize(BinaryReader reader)
        {
            EntityType = (EntityType)reader.ReadInt32();
            DisplayText = reader.ReadString();
            OriginalUrl = reader.ReadString();
            UserId = reader.ReadNullableLong();
            if (reader.ReadBoolean())
                MediaUrl = reader.ReadString();
            StartIndex = reader.ReadInt32();
            EndIndex = reader.ReadInt32();
        }
    }

    public enum EntityType
    {
        Media,
        Urls,
        UserMentions,
        Hashtags
    }
}
