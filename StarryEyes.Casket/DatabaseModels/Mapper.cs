using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Casket.DatabaseModels
{
    public static class Mapper
    {
        public static TwitterUser Map(DatabaseUser user)
        {
            var tu = new TwitterUser
            {
                CreatedAt = user.CreatedAt,
                Description = user.Description,
                DescriptionEntities = null,
                FavoritesCount = user.FavoritesCount,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                Id = user.Id,
                IsContributorsEnabled = user.IsContributorsEnabled,
                IsDefaultProfileImage = user.IsDefaultProfileImage,
                IsGeoEnabled = user.IsGeoEnabled,
                IsProtected = user.IsProtected,
                IsTranslator = user.IsTranslator,
                IsVerified = user.IsVerified,
                Language = user.Language,
                ListedCount = user.ListedCount,
                Location = user.Location,
                Name = user.Name,
                ProfileBackgroundImageUri = new Uri(user.ProfileBackgroundImageUri),
                ProfileBannerUri = new Uri(user.ProfileBannerUri),
                ProfileImageUri = new Uri(user.ProfileImageUri),
                ScreenName = user.ScreenName,
                StatusesCount = user.StatusesCount,
                Url = user.Url,
                UrlEntities = null
            };
            throw new NotImplementedException();
        }

        public static Tuple<DatabaseUser, IEnumerable<DatabaseEntity>> Map(TwitterUser user)
        {
            var tu = new DatabaseUser
            {
                CreatedAt = user.CreatedAt,
                Description = user.Description,
                FavoritesCount = user.FavoritesCount,
                FollowersCount = user.FollowersCount,
                FollowingCount = user.FollowingCount,
                Id = user.Id,
                IsContributorsEnabled = user.IsContributorsEnabled,
                IsDefaultProfileImage = user.IsDefaultProfileImage,
                IsGeoEnabled = user.IsGeoEnabled,
                IsProtected = user.IsProtected,
                IsTranslator = user.IsTranslator,
                IsVerified = user.IsVerified,
                Language = user.Language,
                ListedCount = user.ListedCount,
                Location = user.Location,
                Name = user.Name,
                ProfileBackgroundImageUri = user.ProfileBackgroundImageUri.OriginalString,
                ProfileBannerUri = user.ProfileBannerUri.OriginalString,
                ProfileImageUri = user.ProfileImageUri.OriginalString,
                ScreenName = user.ScreenName,
                StatusesCount = user.StatusesCount,
                Url = user.Url,
            };
            var entities = user.DescriptionEntities.Select(e => Map(user.Id, e, EntityParentType.UserDescription))
                               .Concat(user.UrlEntities.Select(e => Map(user.Id, e, EntityParentType.UserUrl)))
                               .ToArray()
                               .AsEnumerable();
            return Tuple.Create(tu, entities);
        }

        private static DatabaseEntity Map(long parentId, TwitterEntity entity, EntityParentType parent)
        {
            return new DatabaseEntity
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityType = entity.EntityType,
                MediaUrl = entity.MediaUrl,
                OriginalText = entity.OriginalText,
                StartIndex = entity.StartIndex,
                ParentId = parentId,
            };
        }

        public static Tuple<DatabaseStatus, DatabaseUser, IEnumerable<DatabaseEntity>> Map(TwitterStatus status)
        {
            var dbs = new DatabaseStatus
            {
                CreatedAt = status.CreatedAt,
                Id = status.Id,
                InReplyToScreenName = status.InReplyToScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                RecipientId = status.Recipient.Id,
                RetweetOriginalId = status.RetweetedOriginalId,
                Source = status.Source,
                StatusType = status.StatusType,
                Text = status.Text,
                UserId = status.User.Id,
            };
            var ents = status.Entities.Select(e => Map(status.Id, e, EntityParentType.Status));
            var um = Map(status.User);
            var allEnts = ents.Concat(um.Item2).ToArray().AsEnumerable();
            return Tuple.Create(dbs, um.Item1, allEnts);
        }
    }
}
