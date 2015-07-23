using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Casket.DatabaseModels;
using StatusEnts = System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseStatusEntity>;
using UserDescEnts = System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseUserDescriptionEntity>;
using UserUrlEnts = System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseUserUrlEntity>;

namespace StarryEyes.Models.Databases
{
    public static class Mapper
    {
        #region map to database model

        public static Tuple<DatabaseUser, UserDescEnts, UserUrlEnts> Map([NotNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            var tu = new DatabaseUser
            {
                CreatedAt = user.CreatedAt,
                Description = user.Description,
                FavoritesCount = user.FavoritesCount,
                FollowersCount = user.FollowersCount,
                FollowingsCount = user.FollowingsCount,
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

        private static string GetString([CanBeNull] this Uri uri)
        {
            return uri == null ? null : uri.OriginalString;
        }

        private static T Map<T>(long parentId, [NotNull] TwitterEntity entity)
        where T : DatabaseEntity, new()
        {
            if (entity == null) throw new ArgumentNullException("entity");
            return new T
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityType = entity.EntityType,
                MediaUrl = entity.MediaUrl,
                OriginalUrl = entity.OriginalUrl,
                UserId = entity.UserId,
                StartIndex = entity.StartIndex,
                ParentId = parentId,
            };
        }

        public static Tuple<DatabaseStatus, StatusEnts> Map([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            var orig = status.RetweetedStatus ?? status;
            var dbs = new DatabaseStatus
            {
                CreatedAt = status.CreatedAt,
                Id = status.Id,
                BaseId = status.RetweetedStatusId ?? status.Id,
                RetweetId = status.RetweetedStatus != null ? status.Id : (long?)null,
                RetweetOriginalId = status.RetweetedStatusId,
                InReplyToOrRecipientScreenName = status.Recipient != null ? status.Recipient.ScreenName : status.InReplyToScreenName,
                InReplyToStatusId = orig.InReplyToStatusId,
                InReplyToOrRecipientUserId = status.Recipient != null ? status.Recipient.Id : orig.InReplyToUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                BaseSource = orig.Source,
                Source = status.Source,
                StatusType = status.StatusType,
                EntityAidedText = orig.GetEntityAidedText(EntityDisplayMode.LinkUri),
                Text = status.Text,
                UserId = status.User.Id,
                BaseUserId = orig.User.Id,
                RetweeterId = status.RetweetedStatus != null ? status.User.Id : (long?)null,
                RetweetOriginalUserId = status.RetweetedStatus != null ? status.RetweetedStatus.User.Id : (long?)null
            };
            var ent = status.Entities.Guard().Select(e => Map<DatabaseStatusEntity>(status.Id, e));
            return Tuple.Create(dbs, ent);
        }

        #endregion

        #region map to object model

        public static TwitterUser Map([NotNull] DatabaseUser user, [NotNull] UserDescEnts dents,
                                      [NotNull] UserUrlEnts uents)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (dents == null) throw new ArgumentNullException("dents");
            if (uents == null) throw new ArgumentNullException("uents");
            var dent = dents.Memoize();
            var uent = uents.Memoize();
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
                FollowingsCount = user.FollowingsCount,
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
                ProfileBackgroundImageUri = user.ProfileBackgroundImageUri.ParseUri(),
                ProfileBannerUri = user.ProfileBannerUri.ParseUri(),
                ProfileImageUri = user.ProfileImageUri.ParseUri(),
                ScreenName = user.ScreenName,
                StatusesCount = user.StatusesCount,
                Url = user.Url,
                UrlEntities = uent.Select(Map).ToArray()
            };
        }

        public static TwitterEntity Map([NotNull] DatabaseEntity entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            return new TwitterEntity
            {
                DisplayText = entity.DisplayText,
                EndIndex = entity.EndIndex,
                EntityType = entity.EntityType,
                MediaUrl = entity.MediaUrl,
                OriginalUrl = entity.OriginalUrl,
                UserId = entity.UserId,
                StartIndex = entity.StartIndex
            };
        }

        public static TwitterStatus Map([NotNull] DatabaseStatus status, [NotNull] StatusEnts statusEntities,
                                        [CanBeNull] IEnumerable<long> favorers, [CanBeNull] IEnumerable<long> retweeters,
                                        [NotNull] TwitterUser user)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (statusEntities == null) throw new ArgumentNullException("statusEntities");
            if (user == null) throw new ArgumentNullException("user");
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
            return new TwitterStatus(user, status.Text)
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                FavoritedUsers = favorers.Guard().ToArray(),
                Id = status.Id,
                InReplyToScreenName = status.InReplyToOrRecipientScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToOrRecipientUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                RetweetedUsers = retweeters.Guard().ToArray(),
                StatusType = StatusType.Tweet,
                Source = status.Source,
            };
        }

        public static TwitterStatus Map([NotNull] DatabaseStatus status, [NotNull] StatusEnts statusEntities,
                                        [CanBeNull] IEnumerable<long> favorers, [CanBeNull] IEnumerable<long> retweeters,
                                        [NotNull] TwitterStatus originalStatus, [NotNull] TwitterUser user)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (statusEntities == null) throw new ArgumentNullException("statusEntities");
            if (originalStatus == null) throw new ArgumentNullException("originalStatus");
            if (user == null) throw new ArgumentNullException("user");

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
            return new TwitterStatus(user, status.Text)
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                FavoritedUsers = favorers.Guard().ToArray(),
                Id = status.Id,
                InReplyToScreenName = status.InReplyToOrRecipientScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToOrRecipientUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                RetweetedStatus = originalStatus,
                RetweetedStatusId = originalStatus.Id,
                RetweetedUsers = retweeters.Guard().ToArray(),
                Source = status.Source,
                StatusType = StatusType.Tweet,
            };
        }


        public static TwitterStatus Map([NotNull] DatabaseStatus status, [NotNull] StatusEnts statusEntities,
                                        [NotNull] TwitterUser sender, [NotNull] TwitterUser recipient)
        {
            if (status == null) throw new ArgumentNullException("status");
            if (statusEntities == null) throw new ArgumentNullException("statusEntities");
            if (sender == null) throw new ArgumentNullException("sender");
            if (recipient == null) throw new ArgumentNullException("recipient");
            if (status.StatusType != StatusType.DirectMessage)
            {
                throw new ArgumentException("This overload targeting direct message.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            return new TwitterStatus(sender, status.Text)
            {
                CreatedAt = status.CreatedAt,
                Entities = ent.Select(Map).ToArray(),
                Id = status.Id,
                InReplyToScreenName = status.InReplyToOrRecipientScreenName,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToUserId = status.InReplyToOrRecipientUserId,
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                Recipient = recipient,
                StatusType = StatusType.DirectMessage,
            };
        }

        #endregion

        #region map to object model(many)

        public static IEnumerable<TwitterUser> MapMany([NotNull] IEnumerable<DatabaseUser> users,
            [NotNull] Dictionary<long, UserDescEnts> dedic, [NotNull] Dictionary<long, UserUrlEnts> uedic)
        {
            if (users == null) throw new ArgumentNullException("users");
            if (dedic == null) throw new ArgumentNullException("dedic");
            if (uedic == null) throw new ArgumentNullException("uedic");
            return users.Select(user => Map(user, Resolve(dedic, user.Id), Resolve(uedic, user.Id)));
        }

        public static IEnumerable<T> Resolve<T>(IDictionary<long, IEnumerable<T>> dictionary, long id)
        {
            IEnumerable<T> value;
            return dictionary.TryGetValue(id, out value) ? value : Enumerable.Empty<T>();
        }

        #endregion
    }
}
