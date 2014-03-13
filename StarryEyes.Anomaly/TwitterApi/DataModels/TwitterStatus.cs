﻿using System;
using System.Linq;
using System.Text;
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

        public TwitterStatus()
        {
            this.GenerateFromJson = false;
            Entities = new TwitterEntity[0];
        }

        public TwitterStatus(dynamic json)
        {
            this.GenerateFromJson = true;
            this.Id = ((string)json.id_str).ParseLong();
            this.CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
            this.Text = ParsingExtension.ResolveEntity(json.text);
            this.Entities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities));
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
                    this.RetweetedOriginal = new TwitterStatus(json.retweeted_status);
                    this.RetweetedOriginalId = this.RetweetedOriginal.Id;
                    // merge text and entities
                    this.Text = this.RetweetedOriginal.Text;
                    this.Entities = this.RetweetedOriginal.Entities.ToArray();
                }
                if (json.coordinates() && json.coordinates != null)
                {
                    this.Longitude = (double)json.coordinates.coordinates[0];
                    this.Latitude = (double)json.coordinates.coordinates[1];
                }
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
        public TwitterUser User { get; set; }

        /// <summary>
        /// Body of this tweet/message. Escape sequences are already resolved.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Created at of this tweet/message.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #region Status property

        /// <summary>
        /// Source of this tweet. (a.k.a. via, from, ...)
        /// </summary>
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
        public TwitterStatus RetweetedOriginal { get; set; }

        #endregion

        #region Direct messages property

        /// <summary>
        /// Recipient of this message. (ONLY FOR DIRECT MESSAGE)
        /// </summary>
        public TwitterUser Recipient { get; set; }

        #endregion

        #region Activity Controller

        /// <summary>
        /// Favorited users IDs.
        /// </summary>
        public long[] FavoritedUsers { get; set; }

        /// <summary>
        /// Retweeted users IDs.
        /// </summary>
        public long[] RetweetedUsers { get; set; }

        #endregion

        /// <summary>
        /// Entities of this tweet
        /// </summary>
        public TwitterEntity[] Entities { get; set; }

        public string Permalink
        {
            get { return String.Format(TwitterStatusUrl, User.ScreenName, Id); }
        }

        public string FavstarPermalink
        {
            get { return String.Format(FavstarStatusUrl, User.ScreenName, Id); }
        }

        // ReSharper disable InconsistentNaming
        public string STOTString
        {
            get { return this.User.ScreenName + ": " + this.Text + " [" + this.Permalink + "]"; }
        }
        // ReSharper restore InconsistentNaming

        public string GetEntityAidedText(bool showFullUrl = false)
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
                                builder.Append(showFullUrl
                                                   ? ParsingExtension.ResolveEntity(entity.OriginalUrl)
                                                   : ParsingExtension.ResolveEntity(entity.DisplayText));
                                break;
                            case EntityType.Media:
                                if (StatusType == DataModels.StatusType.Tweet)
                                {
                                    builder.Append(showFullUrl
                                                       ? ParsingExtension.ResolveEntity(entity.MediaUrl)
                                                       : ParsingExtension.ResolveEntity(entity.DisplayText));
                                }
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
            return "@" + (User == null ? "[unknown user]" : User.ScreenName) + ": " + this.Text;
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
}