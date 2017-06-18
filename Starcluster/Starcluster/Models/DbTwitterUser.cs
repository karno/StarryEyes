using System;
using Cadena.Data;
using Cadena.Data.Entities;
using JetBrains.Annotations;
using Starcluster.Infrastructures;

namespace Starcluster.Models
{
    public class DbTwitterUser
    {
        public DbTwitterUser()
        {
            ScreenName = String.Empty;
        }

        public DbTwitterUser([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            Id = user.Id;
            ScreenName = user.ScreenName;
            Name = user.Name;
            Description = user.Description;
            Location = user.Location;
            Url = user.Url;
            IsDefaultProfileImage = user.IsDefaultProfileImage;
            ProfileImageUri = user.ProfileImageUri?.ToString();
            ProfileBackgroundImageUri = user.ProfileBackgroundImageUri?.ToString();
            ProfileBannerUri = user.ProfileBannerUri?.ToString();
            IsProtected = user.IsProtected;
            IsVerified = user.IsVerified;
            IsTranslator = user.IsTranslator;
            IsContributorsEnabled = user.IsContributorsEnabled;
            IsGeoEnabled = user.IsGeoEnabled;
            StatusesCount = user.StatusesCount;
            FollowingsCount = user.FollowingsCount;
            FollowersCount = user.FollowersCount;
            FavoritesCount = user.FavoritesCount;
            ListedCount = user.ListedCount;
            Language = user.Language;
            CreatedAt = user.CreatedAt;
        }

        public TwitterUser ToTwitterUser(TwitterUrlEntity[] urlEntities, TwitterEntity[] descriptionEntities)
        {
            return new TwitterUser(
                Id, ScreenName, Name, Description, Location, Url, IsDefaultProfileImage,
                ProfileImageUri.ToUriOrNull(), ProfileBackgroundImageUri.ToUriOrNull(),
                ProfileBannerUri.ToUriOrNull(),
                IsProtected, IsVerified, IsTranslator, IsContributorsEnabled, IsGeoEnabled,
                StatusesCount, FollowingsCount, FollowersCount, FavoritesCount, ListedCount,
                Language, CreatedAt, urlEntities, descriptionEntities);
        }

        [DbPrimaryKey]
        public long Id { get; set; }

        [NotNull]
        public string ScreenName { get; set; }

        [CanBeNull, DbOptional]
        public string Name { get; set; }

        [CanBeNull, DbOptional]
        public string Description { get; set; }

        [CanBeNull, DbOptional]
        public string Location { get; set; }

        [CanBeNull, DbOptional]
        public string Url { get; set; }

        public bool IsDefaultProfileImage { get; set; }

        [CanBeNull, DbOptional]
        public string ProfileImageUri { get; set; }

        [CanBeNull, DbOptional]
        public string ProfileBackgroundImageUri { get; set; }

        [CanBeNull, DbOptional]
        public string ProfileBannerUri { get; set; }

        public bool IsProtected { get; set; }

        public bool IsVerified { get; set; }

        public bool IsTranslator { get; set; }

        public bool IsContributorsEnabled { get; set; }

        public bool IsGeoEnabled { get; set; }

        public long StatusesCount { get; set; }

        public long FollowingsCount { get; set; }

        public long FollowersCount { get; set; }

        public long FavoritesCount { get; set; }

        public long ListedCount { get; set; }

        [CanBeNull, DbOptional]
        public string Language { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}