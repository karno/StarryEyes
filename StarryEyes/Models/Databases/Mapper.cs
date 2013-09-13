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
                ProfileBackgroundImageUri = user.ProfileBackgroundImageUri.GetString(),
                ProfileBannerUri = user.ProfileBannerUri.GetString(),
                ProfileImageUri = user.ProfileImageUri.GetString(),
                ScreenName = user.ScreenName,
                StatusesCount = user.StatusesCount,
                Url = user.Url,
            };
            var entities = user.DescriptionEntities.Guard().Select(e => Map(user.Id, EntityParentType.UserDescription, e))
                               .Concat(user.UrlEntities.Guard().Select(e => Map(user.Id, EntityParentType.UserUrl, e)))
                               .ToArray()
                               .AsEnumerable();
            return Tuple.Create(tu, entities);
        }

        private static string GetString(this Uri uri)
        {
            return uri == null ? null : uri.OriginalString;
        }

        private static DatabaseEntity Map(long parentId, EntityParentType parentType, TwitterEntity entity)
        {
            return new DatabaseEntity
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityType = entity.EntityType,
                EntityParentType = parentType,
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
                RecipientId = status.Recipient != null ? status.Recipient.Id : (long?)null,
                RetweetOriginalId = status.RetweetedOriginalId,
                Source = status.Source,
                StatusType = status.StatusType,
                Text = status.Text,
                UserId = status.User.Id,
            };
            var ents = status.Entities.Select(e => Map(status.Id, EntityParentType.Status, e));
            var um = Map(status.User);
            var allEnts = ents.Concat(um.Item2).ToArray().AsEnumerable();
            return Tuple.Create(dbs, um.Item1, allEnts);
        }

        #endregion

        #region map to object model

        public static TwitterUser Map(DatabaseUser user, IEnumerable<DatabaseEntity> entities)
        {
            var ent = entities.Memoize();
            if (ent.Any(e => e.ParentId != user.Id || (e.EntityParentType != EntityParentType.UserDescription && e.EntityParentType != EntityParentType.UserUrl)))
            {
                throw new ArgumentException("ID mismatched between user and entities.");
            }
            return new TwitterUser
            {
                CreatedAt = user.CreatedAt,
                Description = user.Description,
                DescriptionEntities = ent.Where(e => e.EntityParentType == EntityParentType.UserDescription).Select(Map).ToArray(),
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
                UrlEntities = ent.Where(e => e.EntityParentType == EntityParentType.UserUrl).Select(Map).ToArray()
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
            long[] favorers, long[] retweeters,
            DatabaseUser user, IEnumerable<DatabaseEntity> userEntities)
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
                Source = status.Source,
                Text = status.Text,
                User = Map(user, userEntities),
            };
        }

        public static TwitterStatus Map(DatabaseStatus status, IEnumerable<DatabaseEntity> statusEntities,
            DatabaseStatus originalStatus, IEnumerable<DatabaseEntity> originalEntities,
            long[] favorers, long[] retweeters,
            DatabaseUser user, IEnumerable<DatabaseEntity> userEntities,
            DatabaseUser retweeter, IEnumerable<DatabaseEntity> retweeterEntities)
        {

            if (status.RetweetOriginalId != originalStatus.Id)
            {
                throw new ArgumentException("Retweet id is mismatched.");
            }
            throw new NotImplementedException();
        }



        public static TwitterStatus Map(DatabaseStatus status, IEnumerable<DatabaseEntity> statusEntities,
            DatabaseUser user, IEnumerable<DatabaseEntity> userEntities,
            DatabaseUser recipient, IEnumerable<DatabaseEntity> recipientEntities)
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
                Recipient = Map(recipient, recipientEntities),
                StatusType = StatusType.DirectMessage,
                Text = status.Text,
                User = Map(user, userEntities)
            };
        }


        #endregion
    }
}
