using System;
using System.Linq;
using System.Runtime.Serialization;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Moon.DataModel
{
    /// <summary>
    /// Represents twitter status.
    /// </summary>
    [DataContract]
    public class TwitterStatus : IBinarySerializable
    {
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
        /// Lacking status data (get from search api?)
        /// </summary>
        [DataMember]
        public bool IsDataLacking { get; set; }

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
            writer.Write(IsDataLacking);
            writer.Write(User);
            writer.Write(Text);
            writer.Write(CreatedAt);
            writer.Write(IsFavored);
            writer.Write(Source != null);
            if (Source != null)
                writer.Write(Source);
            writer.Write(InReplyToStatusId);
            writer.Write(InReplyToUserId);
            writer.Write(InReplyToScreenName != null);
            if (InReplyToScreenName != null)
                writer.Write(InReplyToScreenName);
            writer.Write(RetweetedOriginalId);
            writer.Write(Latitude);
            writer.Write(Longitude);
            writer.Write(FavoritedUsers != null);
            if (FavoritedUsers != null)
                writer.Write(FavoritedUsers);
            writer.Write(RetweetedUsers != null);
            if (RetweetedUsers != null)
                writer.Write(RetweetedUsers);
            writer.Write(RetweetedOriginal);
            writer.Write(Recipient);
            writer.Write(Entities);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            Id = reader.ReadInt64();
            StatusType = (StatusType)reader.ReadInt32();
            IsDataLacking = reader.ReadBoolean();
            User = reader.ReadObject<TwitterUser>();
            Text = reader.ReadString();
            CreatedAt = reader.ReadDateTime();
            IsFavored = reader.ReadBoolean();
            if (reader.ReadBoolean())
                Source = reader.ReadString();
            InReplyToStatusId = reader.ReadNullableLong();
            InReplyToUserId = reader.ReadNullableLong();
            if (reader.ReadBoolean())
                InReplyToScreenName = reader.ReadString();
            RetweetedOriginalId = reader.ReadNullableLong();
            Latitude = reader.ReadNullableDouble();
            Longitude = reader.ReadNullableDouble();
            if (reader.ReadBoolean())
                FavoritedUsers = reader.ReadIds().ToArray();
            if (reader.ReadBoolean())
                RetweetedUsers = reader.ReadIds().ToArray();
            RetweetedOriginal = reader.ReadObject<TwitterStatus>();
            Recipient = reader.ReadObject<TwitterUser>();
            Entities = reader.ReadCollection<TwitterEntity>().ToArray();
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