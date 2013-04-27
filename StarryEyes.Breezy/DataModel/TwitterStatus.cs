using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using StarryEyes.Breezy.Util;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Breezy.DataModel
{
    /// <summary>
    /// Represents twitter status.
    /// </summary>
    [DataContract]
    public class TwitterStatus : IBinarySerializable
    {
        public const string TwitterUserUrl = "https://twitter.com/{0}";
        public const string TwitterStatusUrl = "https://twitter.com/{0}/status/{1}";
        public const string FavstarUserUrl = "http://favstar.fm/users/{0}";
        public const string FavstarStatusUrl = "http://favstar.fm/users/{0}/status/{1}";
        public const string TwilogUserUrl = "http://twilog.org/{0}";

        public TwitterStatus()
        {
            Entities = new TwitterEntity[0];
        }

        /// <summary>
        /// Numerical ID of this tweet/message.
        /// </summary>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// Type whether this status is tweet or message.
        /// </summary>
        [DataMember]
        public StatusType StatusType { get; set; }

        /// <summary>
        /// User of this tweet/message.
        /// </summary>
        [DataMember]
        public TwitterUser User { get; set; }

        /// <summary>
        /// Body of this tweet/message.
        /// </summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>
        /// Created at of this tweet/message.
        /// </summary>
        [DataMember]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Favored flag
        /// </summary>
        [DataMember]
        public bool IsFavored { get; set; }

        #region Status property

        /// <summary>
        /// Source of this tweet. (a.k.a. via, from, ...)
        /// </summary>
        [DataMember]
        public string Source { get; set; }

        /// <summary>
        /// Status ID which replied this tweet.
        /// </summary>
        [DataMember]
        public long? InReplyToStatusId { get; set; }

        /// <summary>
        /// User ID which replied this tweet.
        /// </summary>
        [DataMember]
        public long? InReplyToUserId { get; set; }

        /// <summary>
        /// User screen name which replied this tweet.
        /// </summary>
        [DataMember]
        public string InReplyToScreenName { get; set; }

        /// <summary>
        /// Tweet Id which retweeted as this.
        /// </summary>
        [DataMember]
        public long? RetweetedOriginalId { get; set; }

        /// <summary>
        /// Geographic point, represents longitude.
        /// </summary>
        [DataMember]
        public double? Longitude { get; set; }

        /// <summary>
        /// Geographic point, represents latitude.
        /// </summary>
        [DataMember]
        public double? Latitude { get; set; }

        /// <summary>
        /// Favorited users IDs.
        /// </summary>
        [DataMember]
        public long[] FavoritedUsers { get; set; }

        /// <summary>
        /// Retweeted users IDs.
        /// </summary>
        [DataMember]
        public long[] RetweetedUsers { get; set; }

        /// <summary>
        /// Status which represents original of this(retweeted) tweet
        /// </summary>
        [DataMember]
        public TwitterStatus RetweetedOriginal { get; set; }

        #endregion

        #region Direct messages property

        /// <summary>
        /// Recipient of this message. (ONLY FOR DIRECT MESSAGE)
        /// </summary>
        [DataMember]
        public TwitterUser Recipient { get; set; }

        #endregion

        /// <summary>
        /// Entities of this tweet
        /// </summary>
        [DataMember]
        public TwitterEntity[] Entities { get; set; }

        public string UserPermalink
        {
            get { return String.Format(TwitterUserUrl, User.ScreenName); }
        }

        public string FavstarUserPermalink
        {
            get { return String.Format(FavstarUserUrl, User.ScreenName); }
        }

        public string TwilogUserPermalink
        {
            get { return String.Format(TwilogUserUrl, User.ScreenName); }
        }

        public string Permalink
        {
            get { return String.Format(TwitterStatusUrl, User.ScreenName, Id); }
        }

        public string FavstarPermalink
        {
            get { return String.Format(FavstarStatusUrl, User.ScreenName, Id); }
        }

        public string STOTString
        {
            get { return this.User.ScreenName + ": " + this.Text + " [" + this.Permalink + "]"; }
        }

        public string GetEntityAidedText(bool showFullUrl = false)
        {
            var builder = new StringBuilder();
            var status = this;
            if (status.RetweetedOriginal != null)
                status = status.RetweetedOriginal; // change target
            var escaped = ParsingExtension.EscapeEntity(status.Text);
            TwitterEntity prevEntity = null;
            foreach (var entity in status.Entities.Guard().OrderBy(e => e.StartIndex))
            {
                int pidx = 0;
                if (prevEntity != null)
                    pidx = prevEntity.EndIndex;
                if (pidx < entity.StartIndex)
                {
                    // output raw
                    builder.Append(ParsingExtension.ResolveEntity(escaped.Substring(pidx, entity.StartIndex - pidx)));
                }
                switch (entity.EntityType)
                {
                    case EntityType.Hashtags:
                        builder.Append("#" + entity.DisplayText);
                        break;
                    case EntityType.Urls:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.ResolveEntity(entity.OriginalText)
                                           : ParsingExtension.ResolveEntity(entity.DisplayText));
                        break;
                    case EntityType.Media:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.ResolveEntity(entity.MediaUrl)
                                           : ParsingExtension.ResolveEntity(entity.DisplayText));
                        break;
                    case EntityType.UserMentions:
                        builder.Append("@" + entity.DisplayText);
                        break;
                }
                prevEntity = entity;
            }
            if (prevEntity == null)
            {
                builder.Append(ParsingExtension.ResolveEntity(escaped));
            }
            else if (prevEntity.EndIndex < escaped.Length)
            {
                builder.Append(ParsingExtension.ResolveEntity(
                    escaped.Substring(prevEntity.EndIndex, escaped.Length - prevEntity.EndIndex)));
            }
            return builder.ToString();
        }

        /// <summary>
        /// Represent tweet with format: &quot;@user: text&quot;
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "@" + (User == null ? "[unknown user]" : User.ScreenName) + ": " + this.Text;
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((int)StatusType);
            writer.Write(User);
            writer.Write(Text);
            writer.Write(CreatedAt);
            writer.Write(IsFavored);
            writer.Write(Source != null);
            if (Source != null)
            {
                writer.Write(Source);
            }
            writer.Write(InReplyToStatusId);
            writer.Write(InReplyToUserId);
            writer.Write(InReplyToScreenName != null);
            if (InReplyToScreenName != null)
            {
                writer.Write(InReplyToScreenName);
            }
            writer.Write(RetweetedOriginalId);
            writer.Write(Latitude);
            writer.Write(Longitude);
            writer.Write(FavoritedUsers != null);
            if (FavoritedUsers != null)
            {
                writer.Write(FavoritedUsers);
            }
            writer.Write(RetweetedUsers != null);
            if (RetweetedUsers != null)
            {
                writer.Write(RetweetedUsers);
            }
            writer.Write(RetweetedOriginal);
            writer.Write(Recipient);
            writer.Write(Entities != null);
            if (Entities != null)
            {
                writer.Write(Entities);
            }
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            Id = reader.ReadInt64();
            StatusType = (StatusType)reader.ReadInt32();
            User = reader.ReadObject<TwitterUser>();
            Text = reader.ReadString();
            CreatedAt = reader.ReadDateTime();
            IsFavored = reader.ReadBoolean();
            if (reader.ReadBoolean())
            {
                Source = reader.ReadString();
            }
            InReplyToStatusId = reader.ReadNullableLong();
            InReplyToUserId = reader.ReadNullableLong();
            if (reader.ReadBoolean())
            {
                InReplyToScreenName = reader.ReadString();
            }
            RetweetedOriginalId = reader.ReadNullableLong();
            Latitude = reader.ReadNullableDouble();
            Longitude = reader.ReadNullableDouble();
            if (reader.ReadBoolean())
            {
                FavoritedUsers = reader.ReadIds().ToArray();
            }
            if (reader.ReadBoolean())
            {
                RetweetedUsers = reader.ReadIds().ToArray();
            }
            RetweetedOriginal = reader.ReadObject<TwitterStatus>();
            Recipient = reader.ReadObject<TwitterUser>();
            if (reader.ReadBoolean())
            {
                Entities = reader.ReadCollection<TwitterEntity>().ToArray();
            }
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