using System;
using StarryEyes.Casket.Cruds.Scaffolding;

namespace StarryEyes.Casket.DatabaseModels
{
    [DbName("User")]
    public class DatabaseUser : DbModelBase
    {
        [DbPrimaryKey]
        public long Id { get; set; }

        public string ScreenName { get; set; }

        [DbOptional]
        public string Name { get; set; }

        [DbOptional]
        public string Description { get; set; }

        [DbOptional]
        public string Location { get; set; }

        [DbOptional]
        public string Url { get; set; }

        public bool IsDefaultProfileImage { get; set; }

        [DbOptional]
        public string ProfileImageUri { get; set; }

        [DbOptional]
        public string ProfileBackgroundImageUri { get; set; }

        [DbOptional]
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

        [DbOptional]
        public string Language { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
