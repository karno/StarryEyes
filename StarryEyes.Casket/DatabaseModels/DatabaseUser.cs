using System;

namespace StarryEyes.Casket.DatabaseModels
{
    public class DatabaseUser
    {
        public long Id { get; set; }

        public string ScreenName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public string Url { get; set; }

        public bool IsDefaultProfileImage { get; set; }

        public string ProfileImageUri { get; set; }

        public string ProfileBackgroundImageUri { get; set; }

        public string ProfileBannerUri { get; set; }

        public bool IsProtected { get; set; }

        public bool IsVerified { get; set; }

        public bool IsTranslator { get; set; }

        public bool IsContributorsEnabled { get; set; }

        public bool IsGeoEnabled { get; set; }

        public long StatusesCount { get; set; }

        public long FollowingCount { get; set; }

        public long FollowersCount { get; set; }

        public long FavoritesCount { get; set; }

        public long ListedCount { get; set; }

        public string Language { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
