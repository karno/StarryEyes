using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Casket.DatabaseModels;

namespace StarryEyes.Models.Databases
{
    public static class Mapper
    {
        #region map to database model

        public static Tuple<DatabaseUser, IEnumerable<DatabaseUserDescriptionEntity>, IEnumerable<DatabaseUserUrlEntity>> Map(TwitterUser user)
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
                ProfileBackgroundImageUri = user.ProfileBackgroundImageUri.GetString(),
                ProfileBannerUri = user.ProfileBannerUri.GetString(),
                ProfileImageUri = user.ProfileImageUri.GetString(),
                ScreenName = user.ScreenName,
                StatusesCount = user.StatusesCount,
                Url = user.Url,
            };
            var de = user.DescriptionEntities.Guard().Select(e => Map<DatabaseUserDescriptionEntity>(user.Id, e));
            var ue = user.UrlEntities.Guard().Select(e => Map<DatabaseUserUrlEntity>(user.Id, e));
            return Tuple.Create(tu, de, ue);
        }

        private static string GetString(this Uri uri)
        {
            return uri == null ? null : uri.OriginalString;
        }

        private static T Map<T>(long parentId, TwitterEntity entity)
        where T : DatabaseEntity, new()
        {
            return new T
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

        public static Tuple<DatabaseStatus, IEnumerable<DatabaseStatusEntity>> Map(TwitterStatus status)
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
                RecipientId = status.Recipient != null ? status.Recipient.Id : (long?)null,
                RetweetOriginalId = status.RetweetedOriginalId,
                Source = status.Source,
                StatusType = status.StatusType,
                Text = status.Text,
                UserId = status.User.Id,
            };
            var ent = status.Entities.Select(e => Map<DatabaseStatusEntity>(status.Id, e));
            return Tuple.Create(dbs, ent);
        }

        #endregion

        #region map to object model

        public static TwitterUser Map(DatabaseUser user,
            IEnumerable<DatabaseUserDescriptionEntity> descriptionEntities,
            IEnumerable<DatabaseUserUrlEntity> userEntities)
        {
            var dent = descriptionEntities.Memoize();
            var uent = userEntities.Memoize();
            var ent = dent.Cast<DatabaseEntity>().Concat(uent);
            if (ent.Any(e => e.ParentId != user.Id))
            {
                throw new ArgumentException("ID mismatched between user and entities.");
            }
            return new TwitterUser
            {
                CreatedAt = user.CreatedAt,
                Description = user.Description,
                DescriptionEntities = dent.Select(Map).ToArray(),
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
                UrlEntities = uent.Select(Map).ToArray()
            };
        }

        public static TwitterEntity Map(DatabaseEntity entity)
        {
            return new TwitterEntity
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityType = entity.EntityType,
                MediaUrl = entity.MediaUrl,
                OriginalText = entity.OriginalText,
                StartIndex = entity.StartIndex
            };
        }

        public static TwitterStatus Map(DatabaseStatus status, IEnumerable<DatabaseEntity> statusEntities,
            long[] favorers, long[] retweeters, TwitterUser user)
        {
            if (status.StatusType != StatusType.Tweet)
            {
                throw new ArgumentException("This overload targeting normal tweet.");
            }
            if (status.UserId != user.Id)
            {
                throw new ArgumentException("ID mismatched between staus and user.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            return new TwitterStatus
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                FavoritedUsers = favorers,
                Id = status.Id,
                InReplyToScreenName = status.InReplyToScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                RetweetedUsers = retweeters,
                StatusType = StatusType.Tweet,
                Source = status.Source,
                Text = status.Text,
                User = user,
            };
        }

        public static TwitterStatus Map(DatabaseStatus status, IEnumerable<DatabaseEntity> statusEntities,
             long[] favorers, long[] retweeters, TwitterStatus originalStatus, TwitterUser user)
        {

            if (status.RetweetOriginalId != originalStatus.Id)
            {
                throw new ArgumentException("Retweet id is mismatched.");
            }
            if (status.StatusType != StatusType.Tweet)
            {
                throw new ArgumentException("This overload targeting normal tweet.");
            }
            if (status.UserId != user.Id)
            {
                throw new ArgumentException("ID mismatched between staus and user.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            return new TwitterStatus
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                FavoritedUsers = favorers,
                Id = status.Id,
                InReplyToScreenName = status.InReplyToScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                RetweetedOriginal = originalStatus,
                RetweetedOriginalId = originalStatus.Id,
                RetweetedUsers = retweeters,
                Source = status.Source,
                StatusType = StatusType.Tweet,
                Text = status.Text,
                User = user,
            };
        }



        public static TwitterStatus Map(DatabaseStatus status, IEnumerable<DatabaseEntity> statusEntities,
        TwitterUser sender, TwitterUser recipient)
        {
            if (status.StatusType != StatusType.DirectMessage)
            {
                throw new ArgumentException("This overload targeting direct message.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            return new TwitterStatus
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                Id = status.Id,
                InReplyToScreenName = status.InReplyToScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                Recipient = recipient,
                StatusType = StatusType.DirectMessage,
                Text = status.Text,
                User = sender,
            };
        }


        #endregion
    }
}
