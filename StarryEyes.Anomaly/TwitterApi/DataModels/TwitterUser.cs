using System;
using System.IO;
using System.Linq;
using System.Text;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterUser : IBinarySerializable
    {
        public TwitterUser()
        {
        }

        public TwitterUser(dynamic json)
        {
            this.Id = ((string)json.id_str).ParseLong();
            this.ScreenName = json.screen_name;
            this.Name = json.name;
            this.Description = json.description;
            this.Location = json.location;
            this.Url = json.url;
            this.IsDefaultProfileImage = json.default_profile_image;
            this.ProfileImageUri = ((string)json.profile_image_url).ParseUri();
            this.ProfileBackgroundImageUri = ((string)json.profile_background_image_url).ParseUri();
            if (json.profile_banner_url())
            {
                this.ProfileBannerUri = ((string)json.profile_banner_url).ParseUri();
            }
            this.IsProtected = json["protected"];
            this.IsVerified = json.verified;
            this.IsTranslator = json.is_translator;
            this.IsContributorsEnabled = json.contributors_enabled;
            this.IsGeoEnabled = json.geo_enabled;
            this.StatusesCount = (long)json.statuses_count;
            this.FollowingCount = (long)json.friends_count;
            this.FollowersCount = (long)json.followers_count;
            this.FavoritesCount = (long)json.favourites_count;
            this.ListedCount = (long)json.listed_count;
            this.Language = json.lang;
            this.CreatedAt = ((string)json.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat);
            if (json.entities())
            {
                UrlEntities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities.url));
                DescriptionEntities = Enumerable.ToArray(TwitterEntity.GetEntities(json.entities.description));
            }
        }

        public const string TwitterUserUrl = "https://twitter.com/{0}";
        public const string FavstarUserUrl = "http://favstar.fm/users/{0}";
        public const string TwilogUserUrl = "http://twilog.org/{0}";

        /// <summary>
        /// Exactly Numeric ID of this user. (PRIMARY KEY)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ScreenName ( sometimes also call @ID ) of this user.
        /// </summary>
        public string ScreenName { get; set; }

        /// <summary>
        /// Name for the display of this user.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of this user, also calls &quot;Bio&quot;
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Location of this user.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Url of this user. <para />
        /// Warning: This property, named URL but, may not be exactly URI.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Profile image is default or not.
        /// </summary>
        public bool IsDefaultProfileImage { get; set; }

        /// <summary>
        /// Profile image of this user.
        /// </summary>
        public Uri ProfileImageUri { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        public Uri ProfileBackgroundImageUri { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        public Uri ProfileBannerUri { get; set; }

        /// <summary>
        /// Flag for check protected of this user.
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// Flag of this user is verified by twitter official.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Flag of this user works as translator.
        /// </summary>
        public bool IsTranslator { get; set; }

        /// <summary>
        /// Flag of this user using &quot;Writers&quot;
        /// </summary>
        public bool IsContributorsEnabled { get; set; }

        /// <summary>
        /// Flag of this user using &quot;geo&quot; feature.
        /// </summary>
        public bool IsGeoEnabled { get; set; }

        /// <summary>
        /// Amount of tweets of this user.
        /// </summary>
        public long StatusesCount { get; set; }

        /// <summary>
        /// Amount of friends(a.k.a followings) of this user.
        /// </summary>
        public long FollowingCount { get; set; }

        /// <summary>
        /// Amount of followers of this user.
        /// </summary>
        public long FollowersCount { get; set; }

        /// <summary>
        /// Amount of favorites of this user.
        /// </summary>
        public long FavoritesCount { get; set; }

        /// <summary>
        /// Amount of listed by someone of this user.
        /// </summary>
        public long ListedCount { get; set; }

        /// <summary>
        /// Language of this user
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Created time of this user
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Entities of this user url
        /// </summary>
        public TwitterEntity[] UrlEntities { get; set; }

        /// <summary>
        /// Entities of this user description
        /// </summary>
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

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(ScreenName);
            writer.Write(Name ?? String.Empty);
            writer.Write(Description ?? String.Empty);
            writer.Write(Location ?? String.Empty);
            writer.Write(Url ?? String.Empty);
            writer.Write(IsDefaultProfileImage);
            writer.Write(ProfileImageUri);
            writer.Write(ProfileBackgroundImageUri);
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

        public void Deserialize(BinaryReader reader)
        {
            Id = reader.ReadInt64();
            ScreenName = reader.ReadString();
            Name = reader.ReadString();
            Description = reader.ReadString();
            Location = reader.ReadString();
            Url = reader.ReadString();
            IsDefaultProfileImage = reader.ReadBoolean();
            ProfileImageUri = reader.ReadUri();
            ProfileBackgroundImageUri = reader.ReadUri();
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
    }
}