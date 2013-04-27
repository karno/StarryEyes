using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using StarryEyes.Breezy.Util;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Breezy.DataModel
{
    [DataContract]
    public class TwitterUser : IBinarySerializable
    {
        /// <summary>
        /// Exactly Numeric ID of this user. (PRIMARY KEY)
        /// </summary>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// ScreenName ( sometimes also call @ID ) of this user.
        /// </summary>
        [DataMember]
        public string ScreenName { get; set; }

        /// <summary>
        /// Name for the display of this user.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Description of this user, also calls &quot;Bio&quot;
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Location of this user.
        /// </summary>
        [DataMember]
        public string Location { get; set; }

        /// <summary>
        /// Url of this user. <para />
        /// Warning: This property, named URL but, may not be exactly URI.
        /// </summary>
        [DataMember]
        public string Url { get; set; }

        /// <summary>
        /// Profile image is default or not.
        /// </summary>
        [DataMember]
        public bool IsDefaultProfileImage { get; set; }

        /// <summary>
        /// Profile image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileImageUri { get; set; }

        /// <summary>
        /// HttpsProfile image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileImageUriHttps { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileBackgroundImageUri { get; set; }

        /// <summary>
        /// HttpsProfile background image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileBackgroundImageUriHttps { get; set; }

        /// <summary>
        /// Flag for check protected of this user.
        /// </summary>
        [DataMember]
        public bool IsProtected { get; set; }

        /// <summary>
        /// Flag of this user is verified by twitter official.
        /// </summary>
        [DataMember]
        public bool IsVerified { get; set; }

        /// <summary>
        /// Flag of this user works as translator.
        /// </summary>
        [DataMember]
        public bool IsTranslator { get; set; }

        /// <summary>
        /// Flag of this user using &quot;Writers&quot;
        /// </summary>
        [DataMember]
        public bool IsContributorsEnabled { get; set; }

        /// <summary>
        /// Flag of this user using &quot;geo&quot; feature.
        /// </summary>
        [DataMember]
        public bool IsGeoEnabled { get; set; }

        /// <summary>
        /// Amount of tweets of this user.
        /// </summary>
        [DataMember]
        public long StatusesCount { get; set; }

        /// <summary>
        /// Amount of friends(a.k.a followings) of this user.
        /// </summary>
        [DataMember]
        public long FriendsCount { get; set; }

        /// <summary>
        /// Amount of followers of this user.
        /// </summary>
        [DataMember]
        public long FollowersCount { get; set; }

        /// <summary>
        /// Amount of favorites of this user.
        /// </summary>
        [DataMember]
        public long FavoritesCount { get; set; }

        /// <summary>
        /// Amount of listed by someone of this user.
        /// </summary>
        [DataMember]
        public long ListedCount { get; set; }

        /// <summary>
        /// Language of this user
        /// </summary>
        [DataMember]
        public string Language { get; set; }

        /// <summary>
        /// Created time of this user
        /// </summary>
        [DataMember]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Entities of this tweet
        /// </summary>
        [DataMember]
        public TwitterEntity[] Entities { get; set; }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(ScreenName);
            writer.Write(Name ?? String.Empty);
            writer.Write(CreatedAt);
            writer.Write(Description ?? String.Empty);
            writer.Write(Location ?? String.Empty);
            writer.Write(Url ?? String.Empty);
            writer.Write(ProfileImageUri);
            writer.Write(IsProtected);
            writer.Write(IsVerified);
            writer.Write(IsTranslator);
            writer.Write(IsContributorsEnabled);
            writer.Write(IsGeoEnabled);
            writer.Write(StatusesCount);
            writer.Write(FriendsCount);
            writer.Write(FollowersCount);
            writer.Write(FavoritesCount);
            writer.Write(ListedCount);
            writer.Write(Language ?? String.Empty);
            writer.Write(Entities);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            Id = reader.ReadInt64();
            ScreenName = reader.ReadString();
            Name = reader.ReadString();
            CreatedAt = reader.ReadDateTime();
            Description = reader.ReadString();
            Location = reader.ReadString();
            Url = reader.ReadString();
            ProfileImageUri = reader.ReadUri();
            IsProtected = reader.ReadBoolean();
            IsVerified = reader.ReadBoolean();
            IsTranslator = reader.ReadBoolean();
            IsContributorsEnabled = reader.ReadBoolean();
            IsGeoEnabled = reader.ReadBoolean();
            StatusesCount = reader.ReadInt64();
            FriendsCount = reader.ReadInt64();
            FollowersCount = reader.ReadInt64();
            FavoritesCount = reader.ReadInt64();
            ListedCount = reader.ReadInt64();
            Language = reader.ReadString();
            Entities = reader.ReadCollection<TwitterEntity>().ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.Id == ((TwitterUser)obj).Id;
        }

        public string GetEntityAidedText(bool showFullUrl = false)
        {
            var builder = new StringBuilder();
            var escaped = ParsingExtension.EscapeEntity(this.Description);
            TwitterEntity prevEntity = null;
            foreach (var entity in this.Entities.Guard().OrderBy(e => e.StartIndex))
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

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}