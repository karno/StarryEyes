using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    /// <summary>
    /// Represents twitter status.
    /// </summary>
    public class TwitterStatus
    {
        public const string TwitterStatusUrl = "https://twitter.com/{0}/status/{1}";
        public const string FavstarStatusUrl = "http://favstar.fm/users/{0}/status/{1}";

        public TwitterStatus([@CanBeNull] TwitterUser user, [@CanBeNull] string text)
        {
            GenerateFromJson = false;
            User = user;
            Text = text;
        }

        public TwitterStatus(Cadena.Data.TwitterStatus cts)
        {
            GenerateFromJson = true;
            Id = cts.Id;
            CreatedAt = cts.CreatedAt;


            SetTextAndEntities(json);
            if (json.extended_tweet())
            {
                SetTextAndEntities(json.extended_tweet);
            }

            if (json.recipient())
            {
                // THIS IS DIRECT MESSAGE!
                StatusType = StatusType.DirectMessage;
                User = new TwitterUser(json.sender);
                Recipient = new TwitterUser(json.recipient);
            }
            else
            {
                StatusType = StatusType.Tweet;
                User = new TwitterUser(json.user);
                Source = json.source;
                if (json.in_reply_to_status_id_str())
                {
                    InReplyToStatusId = ((string)json.in_reply_to_status_id_str).ParseNullableId();
                }
                if (json.in_reply_to_user_id_str())
                {
                    InReplyToUserId = ((string)json.in_reply_to_user_id_str).ParseNullableId();
                }
                if (json.in_reply_to_screen_name())
                {
                    InReplyToScreenName = json.in_reply_to_screen_name;
                }
                if (json.retweeted_status())
                {
                    var original = new TwitterStatus(json.retweeted_status);
                    RetweetedOriginal = original;
                    RetweetedOriginalId = original.Id;
                    // merge text and entities
                    Text = original.Text;
                    Entities = original.Entities.Guard().ToArray();
                }
                if (json.coordinates() && json.coordinates != null)
                {
                    Longitude = (double)json.coordinates.coordinates[0];
                    Latitude = (double)json.coordinates.coordinates[1];
                }
            }
        }

        public TwitterStatus(dynamic json)
        {
            GenerateFromJson = true;
            Id = ((string)json.id_str).ParseLong();
            CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);

            SetTextAndEntities(json);
            if (json.extended_tweet())
            {
                SetTextAndEntities(json.extended_tweet);
            }

            if (json.recipient())
            {
                // THIS IS DIRECT MESSAGE!
                StatusType = StatusType.DirectMessage;
                User = new TwitterUser(json.sender);
                Recipient = new TwitterUser(json.recipient);
            }
            else
            {
                StatusType = StatusType.Tweet;
                User = new TwitterUser(json.user);
                Source = json.source;
                if (json.in_reply_to_status_id_str())
                {
                    InReplyToStatusId = ((string)json.in_reply_to_status_id_str).ParseNullableId();
                }
                if (json.in_reply_to_user_id_str())
                {
                    InReplyToUserId = ((string)json.in_reply_to_user_id_str).ParseNullableId();
                }
                if (json.in_reply_to_screen_name())
                {
                    InReplyToScreenName = json.in_reply_to_screen_name;
                }
                if (json.retweeted_status())
                {
                    var original = new TwitterStatus(json.retweeted_status);
                    RetweetedOriginal = original;
                    RetweetedOriginalId = original.Id;
                    // merge text and entities
                    Text = original.Text;
                    Entities = original.Entities.Guard().ToArray();
                }
                if (json.coordinates() && json.coordinates != null)
                {
                    Longitude = (double)json.coordinates.coordinates[0];
                    Latitude = (double)json.coordinates.coordinates[1];
                }
            }
        }

        private void SetTextAndEntities(dynamic root)
        {
            if (root.full_text())
            {
                Text = ParsingExtension.ResolveEntity(root.full_text);
            }
            else if (root.text())
            {
                Text = ParsingExtension.ResolveEntity(root.text);
            }
            if (root.extended_entities())
            {
                // get correctly typed entities array
                var orgEntities = (TwitterEntity[])Enumerable.ToArray(TwitterEntity.GetEntities(root.entities));
                var extEntities =
                    (TwitterEntity[])Enumerable.ToArray(TwitterEntity.GetEntities(root.extended_entities));

                // merge entities
                Entities = orgEntities
                    .Where(e => e.EntityType != EntityType.Media)
                    .Concat(extEntities) // extended entities contains media entities only.
                    .ToArray();
            }
            else if (root.entities())
            {
                Entities = Enumerable.ToArray(TwitterEntity.GetEntities(root.entities));
            }
            else
            {
                Entities = new TwitterEntity[0];
            }
        }

        public bool GenerateFromJson { get; private set; }

        /// <summary>
        /// Numerical ID of this tweet/message.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Type whether this status is tweet or message.
        /// </summary>
        public StatusType StatusType { get; set; }

        /// <summary>
        /// User of this tweet/message.
        /// </summary>
        [@CanBeNull]
        public TwitterUser User { get; private set; }

        /// <summary>
        /// Body of this tweet/message. Escape sequences are already resolved.
        /// </summary>
        [@CanBeNull]
        public string Text { get; private set; }

        /// <summary>
        /// Created at of this tweet/message.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #region Status property

        /// <summary>
        /// Source of this tweet. (a.k.a. via, from, ...)
        /// </summary>
        [CanBeNullAttribute]
        public string Source { get; set; }

        /// <summary>
        /// Status ID which replied this tweet.
        /// </summary>
        public long? InReplyToStatusId { get; set; }

        /// <summary>
        /// User ID which replied this tweet.
        /// </summary>
        public long? InReplyToUserId { get; set; }

        /// <summary>
        /// User screen name which replied this tweet.
        /// </summary>
        [CanBeNullAttribute]
        public string InReplyToScreenName { get; set; }

        /// <summary>
        /// Tweet Id which retweeted as this.
        /// </summary>
        public long? RetweetedOriginalId { get; set; }

        /// <summary>
        /// Geographic point, represents longitude.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Geographic point, represents latitude.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Status which represents original of this(retweeted) tweet
        /// </summary>
        [CanBeNullAttribute]
        public TwitterStatus RetweetedOriginal { get; set; }

        #endregion Status property

        #region Direct messages property

        /// <summary>
        /// Recipient of this message. (ONLY FOR DIRECT MESSAGE)
        /// </summary>
        [CanBeNullAttribute]
        public TwitterUser Recipient { get; set; }

        #endregion Direct messages property

        #region Activity Controller

        /// <summary>
        /// Favorited users IDs.
        /// </summary>
        [CanBeNullAttribute]
        public long[] FavoritedUsers { get; set; }

        /// <summary>
        /// Retweeted users IDs.
        /// </summary>
        [CanBeNullAttribute]
        public long[] RetweetedUsers { get; set; }

        #endregion Activity Controller

        /// <summary>
        /// Entities of this tweet
        /// </summary>
        [CanBeNullAttribute]
        public TwitterEntity[] Entities { get; set; }

        [@CanBeNull]
        public string Permalink
        {
            get { return String.Format(TwitterStatusUrl, User.ScreenName, Id); }
        }

        [@CanBeNull]
        public string FavstarPermalink
        {
            get { return String.Format(FavstarStatusUrl, User.ScreenName, Id); }
        }

        // ReSharper disable InconsistentNaming
        [@CanBeNull]
        public string STOTString
        {
            get { return "@" + User.ScreenName + ": " + Text + " [" + Permalink + "]"; }
        }
        // ReSharper restore InconsistentNaming

        [@CanBeNull]
        public string GetEntityAidedText(EntityDisplayMode displayMode = EntityDisplayMode.DisplayText)
        {
            try
            {
                var builder = new StringBuilder();
                var status = this;
                if (status.RetweetedOriginal != null)
                {
                    // change target
                    status = status.RetweetedOriginal;
                }
                foreach (var description in TextEntityResolver.ParseText(status))
                {
                    if (!description.IsEntityAvailable)
                    {
                        builder.Append(description.Text);
                    }
                    else
                    {
                        var entity = description.Entity;
                        switch (entity.EntityType)
                        {
                            case EntityType.Hashtags:
                                builder.Append("#" + entity.DisplayText);
                                break;
                            case EntityType.Urls:
                                // url entity:
                                // display_url: example.com/CUTTED OFF...
                                // original_url => expanded_url: example.com/full_original_url
                                builder.Append(displayMode == EntityDisplayMode.DisplayText
                                    ? ParsingExtension.ResolveEntity(entity.DisplayText)
                                    : ParsingExtension.ResolveEntity(entity.OriginalUrl));
                                break;
                            case EntityType.Media:
                                // media entity:
                                // display_url: pic.twitter.com/IMAGE_ID
                                // media_url: pbs.twimg.com/media/ACTUAL_IMAGE_RESOURCE_ID
                                // url: t.co/IMAGE_ID
                                builder.Append(
                                    displayMode == EntityDisplayMode.DisplayText
                                        ? ParsingExtension.ResolveEntity(entity.DisplayText)
                                        : displayMode == EntityDisplayMode.LinkUri
                                            ? ParsingExtension.ResolveEntity(entity.DisplayText)
                                            : ParsingExtension.ResolveEntity(entity.MediaUrl));
                                break;
                            case EntityType.UserMentions:
                                builder.Append("@" + entity.DisplayText);
                                break;
                        }
                    }
                }
                return builder.ToString();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Parse Error! : " + Text);
                if (Entities == null)
                {
                    sb.AppendLine("Entities: null");
                }
                else
                {
                    sb.Append("Entities: ");
                    Entities.OrderBy(e => e.StartIndex)
                            .Select(e => e.StartIndex + "- " + e.EndIndex + " : " + e.DisplayText)
                            .ForEach(s => sb.AppendLine("    " + s));
                }
                throw new ArgumentOutOfRangeException(sb.ToString(), ex);
            }
        }

        /// <summary>
        /// Represent tweet with format: &quot;@user: text&quot;
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "@" + User.ScreenName + ": " + GetEntityAidedText();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Id == ((TwitterStatus)obj).Id;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    /// <summary>
    /// Type of status
    /// </summary>
    public enum StatusType
    {
        /// <summary>
        /// Status is normal tweet.
        /// </summary>
        Tweet,

        /// <summary>
        /// Status is direct message.
        /// </summary>
        DirectMessage,
    }

    public enum EntityDisplayMode
    {
        DisplayText,
        LinkUri,
        MediaUri
    }
}