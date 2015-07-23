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

        public TwitterStatus([NotNull] TwitterUser user, [NotNull] string text)
        {
            this.GeneratedFromJson = false;
            this.User = user;
            this.Text = text;
        }

        public TwitterStatus(dynamic json)
        {
            this.GeneratedFromJson = true;
            this.Id = ((string)json.id_str).ParseLong();
            this.CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
            this.Text = ParsingExtension.ResolveEntity(json.text);
            if (json.extended_entities())
            {
                // get correctly typed entities array
                var orgEntities = (TwitterEntity[])Enumerable.ToArray(TwitterEntity.GetEntities(json.entities));
                var extEntities = (TwitterEntity[])Enumerable.ToArray(TwitterEntity.GetEntities(json.extended_entities));

                // merge entities
                this.Entities = orgEntities.Where(e => e.EntityType != EntityType.Media)
                                           .Concat(extEntities) // extended entities contains media entities only.
                                           .ToArray();
            }
            else if (json.entities())
            {
                this.Entities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities));
            }
            else
            {
                this.Entities = new TwitterEntity[0];
            }
            if (json.recipient())
            {
                // THIS IS DIRECT MESSAGE!
                this.StatusType = StatusType.DirectMessage;
                this.User = new TwitterUser(json.sender);
                this.Recipient = new TwitterUser(json.recipient);
            }
            else
            {
                this.StatusType = StatusType.Tweet;
                this.User = new TwitterUser(json.user);
                this.Source = json.source;
                if (json.in_reply_to_status_id_str())
                {
                    this.InReplyToStatusId = ((string)json.in_reply_to_status_id_str).ParseNullableId();
                }
                if (json.in_reply_to_user_id_str())
                {
                    this.InReplyToUserId = ((string)json.in_reply_to_user_id_str).ParseNullableId();
                }
                if (json.in_reply_to_screen_name())
                {
                    this.InReplyToScreenName = json.in_reply_to_screen_name;
                }
                if (json.retweeted_status())
                {
                    var retweeted = new TwitterStatus(json.retweeted_status);
                    this.RetweetedStatus = retweeted;
                    this.RetweetedStatusId = retweeted.Id;
                    // merge text and entities
                    this.Text = retweeted.Text;
                    this.Entities = retweeted.Entities.Guard().ToArray();
                }
                if (json.quoted_status())
                {
                    var quoted = new TwitterStatus(json.quoted_status);
                    this.QuotedStatus = quoted;
                    this.QuotedStatusId = quoted.Id;
                }
                if (json.coordinates() && json.coordinates != null)
                {
                    this.Longitude = (double)json.coordinates.coordinates[0];
                    this.Latitude = (double)json.coordinates.coordinates[1];
                }
            }
        }

        public bool GeneratedFromJson { get; private set; }

        /// <summary>
        /// Sequential ID of this tweet/message.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The flag indicated whether this status is a tweet or a message.
        /// </summary>
        public StatusType StatusType { get; set; }

        /// <summary>
        /// Author of this tweet/message.
        /// </summary>
        [NotNull]
        public TwitterUser User { get; private set; }

        /// <summary>
        /// Body of this tweet/message. Escape characters are already resolved.
        /// </summary>
        [NotNull]
        public string Text { get; private set; }

        /// <summary>
        /// Created timestamp of this tweet/message.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #region Property for statuses

        /// <summary>
        /// Source of this tweet. (a.k.a. via, from, ...)
        /// </summary>
        [CanBeNull]
        public string Source { get; set; }

        /// <summary>
        /// Status ID that is replied from this tweet.
        /// </summary>
        public long? InReplyToStatusId { get; set; }

        /// <summary>
        /// User ID that is replied from this tweet.
        /// </summary>
        public long? InReplyToUserId { get; set; }

        /// <summary>
        /// User screen name that is replied from this tweet.
        /// </summary>
        [CanBeNull]
        public string InReplyToScreenName { get; set; }

        /// <summary>
        /// Latitude of geographic point that is associated with this status.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude of geographic point that is associated with this status.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// The id of the status that is retweeted by this status
        /// </summary>
        public long? RetweetedStatusId { get; set; }

        /// <summary>
        /// The status that is retweeted by this status
        /// </summary>
        [CanBeNull]
        public TwitterStatus RetweetedStatus { get; set; }

        /// <summary>
        /// The id of the status that is quoted by this status
        /// </summary>
        public long? QuotedStatusId { get; set; }

        /// <summary>
        /// The status that is quoted by this status
        /// </summary>
        [CanBeNull]
        public TwitterStatus QuotedStatus { get; set; }
        #endregion

        #region Properties for direct messages

        /// <summary>
        /// Recipient of this message. (ONLY FOR DIRECT MESSAGE)
        /// </summary>
        [CanBeNull]
        public TwitterUser Recipient { get; set; }

        #endregion

        #region Properties for additional information

        /// <summary>
        /// Favorited users IDs.
        /// </summary>
        [CanBeNull]
        public long[] FavoritedUsers { get; set; }

        /// <summary>
        /// Retweeted users IDs.
        /// </summary>
        [CanBeNull]
        public long[] RetweetedUsers { get; set; }

        #endregion

        /// <summary>
        /// Entity objects of this tweet
        /// </summary>
        [CanBeNull]
        public TwitterEntity[] Entities { get; set; }

        /// <summary>
        /// Web URL for accessing this status
        /// </summary>
        [NotNull]
        public string Permalink
        {
            get { return String.Format(TwitterStatusUrl, User.ScreenName, Id); }
        }

        /// <summary>
        /// Favstar URL for accessing this status
        /// </summary>
        [NotNull]
        public string FavstarPermalink
        {
            get { return String.Format(FavstarStatusUrl, User.ScreenName, Id); }
        }

        // ReSharper disable InconsistentNaming
        [NotNull]
        public string STOTString
        {
            get
            {
                return "@" + this.User.ScreenName + ": " + this.Text + " [" + this.Permalink + "]";
            }
        }
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Get entity-applied text
        /// </summary>
        /// <param name="displayMode">switch of replacer text</param>
        /// <returns></returns>
        [NotNull]
        public string GetEntityAidedText(EntityDisplayMode displayMode = EntityDisplayMode.DisplayText)
        {
            try
            {
                var builder = new StringBuilder();
                var status = this;
                if (status.RetweetedStatus != null)
                {
                    // change target
                    status = status.RetweetedStatus;
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
        /// Get formatted tweet: &quot;@user: text&quot;
        /// </summary>
        public override string ToString()
        {
            return "@" + User.ScreenName + ": " + this.GetEntityAidedText();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return this.Id == ((TwitterStatus)obj).Id;
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