using System;

namespace StarryEyes.Casket.DataModels
{
    public class DbUser
    {
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
        public string ProfileImage { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        public string ProfileBackgroundImage { get; set; }

        /// <summary>
        /// Profile background image of this user.
        /// </summary>
        public string ProfileBanner { get; set; }

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

        public override bool Equals(object obj)
        {
            var casted = obj as DbUser;
            return casted != null && this.Id == casted.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
