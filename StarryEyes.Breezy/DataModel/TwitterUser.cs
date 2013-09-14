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
        public const string TwitterUserUrl = "https://twitter.com/{0}";
        public const string FavstarUserUrl = "http://favstar.fm/users/{0}";
        public const string TwilogUserUrl = "http://twilog.org/{0}";

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
        /// Profile background image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileBannerUri { get; set; }

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
        public long FollowingCount { get; set; }

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
        /// Entities of this user url
        /// </summary>
        [DataMember]
        public TwitterEntity[] UrlEntities { get; set; }

        /// <summary>
        /// Entities of this user description
        /// </summary>
        [DataMember]
        public TwitterEntity[] DescriptionEntities { get; set; }

        public string UserPermalink
        {
            get { return String.Format(TwitterUserUrl, ScreenName); }
        }

        public string FavstarUserPermalink
        {
            get { return String.Format(FavstarUserUrl, ScreenName); }
        }

        public string TwilogUserPermalink
        {
            get { return String.Format(TwilogUserUrl, ScreenName); }
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(ScreenName);
            writer.Write(Name ?? String.Empty);
            writer.Write(Description ?? String.Empty);
            writer.Write(Location ?? String.Empty);
            writer.Write(Url ?? String.Empty);
            writer.Write(IsDefaultProfileImage);
            writer.Write(ProfileImageUri);
            writer.Write(ProfileImageUriHttps);
            writer.Write(ProfileBackgroundImageUri);
            writer.Write(ProfileBackgroundImageUriHttps);
            writer.Write(ProfileBannerUri);
            writer.Write(IsProtected);
            writer.Write(IsVerified);
            writer.Write(IsTranslator);
            writer.Write(IsContributorsEnabled);
            writer.Write(IsGeoEnabled);
            writer.Write(StatusesCount);
            writer.Write(FollowingCount);
            writer.Write(FollowersCount);
            writer.Write(FavoritesCount);
            writer.Write(ListedCount);
            writer.Write(Language ?? String.Empty);
            writer.Write(CreatedAt);
            writer.Write(UrlEntities != null);
            if (UrlEntities != null)
            {
                writer.Write(UrlEntities);
            }
            writer.Write(DescriptionEntities != null);
            if (DescriptionEntities != null)
            {
                writer.Write(DescriptionEntities);
            }
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            Id = reader.ReadInt64();
            ScreenName = reader.ReadString();
            Name = reader.ReadString();
            Description = reader.ReadString();
            Location = reader.ReadString();
            Url = reader.ReadString();
            IsDefaultProfileImage = reader.ReadBoolean();
            ProfileImageUri = reader.ReadUri();
            ProfileImageUriHttps = reader.ReadUri();
            ProfileBackgroundImageUri = reader.ReadUri();
            ProfileBackgroundImageUriHttps = reader.ReadUri();
            ProfileBannerUri = reader.ReadUri();
            IsProtected = reader.ReadBoolean();
            IsVerified = reader.ReadBoolean();
            IsTranslator = reader.ReadBoolean();
            IsContributorsEnabled = reader.ReadBoolean();
            IsGeoEnabled = reader.ReadBoolean();
            StatusesCount = reader.ReadInt64();
            FollowingCount = reader.ReadInt64();
            FollowersCount = reader.ReadInt64();
            FavoritesCount = reader.ReadInt64();
            ListedCount = reader.ReadInt64();
            Language = reader.ReadString();
            CreatedAt = reader.ReadDateTime();
            if (reader.ReadBoolean())
            {
                UrlEntities = reader.ReadCollection<TwitterEntity>().ToArray();
            }
            if (reader.ReadBoolean())
            {
                DescriptionEntities = reader.ReadCollection<TwitterEntity>().ToArray();
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.Id == ((TwitterUser)obj).Id;
        }

        public string GetEntityAidedUrl()
        {
            if (this.UrlEntities != null)
            {
                var entity = this.UrlEntities.FirstOrDefault(u => u.EntityType == EntityType.Urls);
                if (entity != null)
                {
                    return entity.OriginalText;
                }
            }
            return Url;
        }

        public string GetEntityAidedDescription(bool showFullUrl = false)
        {
            var builder = new StringBuilder();
            var escaped = ParsingExtension.EscapeEntity(this.Description);
            TwitterEntity prevEntity = null;
            foreach (var entity in this.DescriptionEntities.Guard().OrderBy(e => e.StartIndex))
            {
                var pidx = 0;
                if (prevEntity != null)
                    pidx = prevEntity.EndIndex;
                if (pidx < entity.StartIndex)
                {
                    // output raw
                    builder.Append(ParsingExtension.UnescapeEntity(escaped.Substring(pidx, entity.StartIndex - pidx)));
                }
                switch (entity.EntityType)
                {
                    case EntityType.Hashtags:
                        builder.Append("#" + entity.DisplayText);
                        break;
                    case EntityType.Urls:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.UnescapeEntity(entity.OriginalText)
                                           : ParsingExtension.UnescapeEntity(entity.DisplayText));
                        break;
                    case EntityType.Media:
                        builder.Append(showFullUrl
                                           ? ParsingExtension.UnescapeEntity(entity.MediaUrl)
                                           : ParsingExtension.UnescapeEntity(entity.DisplayText));
                        break;
                    case EntityType.UserMentions:
                        builder.Append("@" + entity.DisplayText);
                        break;
                }
                prevEntity = entity;
            }
            if (prevEntity == null)
            {
                builder.Append(ParsingExtension.UnescapeEntity(escaped));
            }
            else if (prevEntity.EndIndex < escaped.Length)
            {
                builder.Append(ParsingExtension.UnescapeEntity(
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