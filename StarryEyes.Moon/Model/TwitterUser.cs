using System;
using System.Runtime.Serialization;
using StarryEyes.Vanille.Serialization;

namespace StarryEyes.Moon.DataModel
{
    [DataContract]
    public class TwitterUser : IBinarySerializable
    {
        public TwitterUser() { }

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
        /// Lacking data (assert this contains only ID and ScreenName and some features, 
        /// so should be reload for showing all data)
        /// </summary>
        [DataMember]
        public bool IsDataLacking { get; set; }

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
        /// Profile image of this user.
        /// </summary>
        [DataMember]
        public Uri ProfileImageUri { get; set; }

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

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(ScreenName);
            writer.Write(IsDataLacking);
            writer.Write(Name ?? String.Empty);
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
            if (!IsDataLacking)
                writer.Write(CreatedAt);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            Id = reader.ReadInt64();
            ScreenName = reader.ReadString();
            IsDataLacking = reader.ReadBoolean();
            Name = reader.ReadString();
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
            if (!IsDataLacking)
                CreatedAt = reader.ReadDateTime();
        }
    }
}