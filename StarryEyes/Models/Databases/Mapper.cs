using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using Cadena.Data.Entities;
using Cadena.Util;
using JetBrains.Annotations;
using StarryEyes.Casket.DatabaseModels;
using StatusEnts = System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseStatusEntity>;
using UserDescEnts =
    System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseUserDescriptionEntity>;
using UserUrlEnts = System.Collections.Generic.IEnumerable<StarryEyes.Casket.DatabaseModels.DatabaseUserUrlEntity>;

namespace StarryEyes.Models.Databases
{
    public static class Mapper
    {
        #region map to database model

        public static Tuple<DatabaseUser, UserDescEnts, UserUrlEnts> Map([CanBeNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
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
                Url = user.Url
            };
            var de = user.DescriptionEntities.Guard().Select(e => Map<DatabaseUserDescriptionEntity>(user.Id, e));
            var ue = user.UrlEntities.Guard().Select(e => Map<DatabaseUserUrlEntity>(user.Id, e));
            return Tuple.Create(tu, de, ue);
        }

        private static string GetString([CanBeNull] this Uri uri)
        {
            return uri == null ? null : uri.OriginalString;
        }

        private static T Map<T>(long parentId, [CanBeNull] TwitterEntity entity)
            where T : DatabaseEntity, new()
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            EntityType et;
            string murl = null;
            string ourl = null;
            long? uid = null;
            if (entity is TwitterUrlEntity ue)
            {
                et = EntityType.Urls;
                murl = ue.ExpandedUrl;
                ourl = ue.ExpandedUrl;
            }
            else if (entity is TwitterMediaEntity me)
            {
                et = EntityType.Hashtags;
                uid = me.Id;
            }
            else if (entity is TwitterHashtagEntity)
            {
                et = EntityType.Hashtags;
            }
            else if (entity is TwitterSymbolEntity)
            {
                et = EntityType.Hashtags;
            }
            else if (entity is TwitterUserMentionEntity re)
            {
                et = EntityType.UserMentions;
                uid = re.Id;
            }
            else
            {
                throw new ArgumentException($"mapper did not support type {entity.GetType().FullName} yet.");
            }

            return new T
            {
                DisplayText = entity.DisplayText,
                StartIndex = entity.Indices.Item1,
                EndIndex = entity.Indices.Item2,
                EntityType = et,
                MediaUrl = murl,
                OriginalUrl = ourl,
                UserId = uid,
                ParentId = parentId
            };
        }

        public static Tuple<DatabaseStatus, StatusEnts> Map([CanBeNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            var orig = status.RetweetedStatus ?? status;
            var dbs = new DatabaseStatus
            {
                CreatedAt = status.CreatedAt,
                Id = status.Id,
                BaseId = (status.RetweetedStatus ?? status).Id,
                RetweetId = status.RetweetedStatus != null ? status.Id : (long?)null,
                RetweetOriginalId = status.RetweetedStatus?.Id,
                InReplyToOrRecipientScreenName = status.Recipient?.ScreenName ?? status.InReplyToScreenName,
                InReplyToStatusId = orig.InReplyToStatusId,
                InReplyToOrRecipientUserId = status.Recipient?.Id ?? orig.InReplyToUserId,
                Latitude = status.Coordinates?.Item1,
                Longitude = status.Coordinates?.Item2,
                BaseSource = orig.Source,
                Source = status.Source,
                StatusType = status.StatusType,
                EntityAidedText = orig.GetEntityAidedText(EntityDisplayMode.LinkUri),
                Text = status.Text,
                UserId = status.User.Id,
                BaseUserId = orig.User.Id,
                QuoteId = status.QuotedStatus?.Id,
                QuoteUserId = status.QuotedStatus?.User.Id,
                RetweeterId = status.RetweetedStatus != null ? status.User.Id : (long?)null,
                RetweetOriginalUserId = status.RetweetedStatus?.User.Id,
                DisplayTextRangeBegin = status.DisplayTextRange?.Item1,
                DisplayTextRangeEnd = status.DisplayTextRange?.Item2
            };
            var ent = status.Entities.Guard().Select(e => Map<DatabaseStatusEntity>(status.Id, e));
            return Tuple.Create(dbs, ent);
        }

        #endregion map to database model

        #region map to object model

        public static TwitterUser Map([CanBeNull] DatabaseUser user, [CanBeNull] UserDescEnts dents,
            [CanBeNull] UserUrlEnts uents)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (dents == null) throw new ArgumentNullException(nameof(dents));
            if (uents == null) throw new ArgumentNullException(nameof(uents));
            var dent = dents.Memoize();
            var uent = uents.Memoize();
            var ent = dent.Cast<DatabaseEntity>().Concat(uent);
            if (ent.Any(e => e.ParentId != user.Id))
            {
                throw new ArgumentException("ID mismatched between user and entities.");
            }
            return new TwitterUser(user.Id, user.ScreenName, user.Name,
                user.Description, user.Location, user.Url, user.IsDefaultProfileImage,
                user.ProfileImageUri.ParseUri(), user.ProfileBackgroundImageUri.ParseUri(),
                user.ProfileBannerUri.ParseUri(), user.IsProtected, user.IsVerified, user.IsTranslator,
                user.IsContributorsEnabled, user.IsGeoEnabled, user.StatusesCount, user.FollowingsCount,
                user.FollowersCount, user.FavoritesCount, user.ListedCount, user.Language, user.CreatedAt,
                uent.Select(Map).OfType<TwitterUrlEntity>().ToArray(), dent.Select(Map).ToArray());
        }


        public static TwitterEntity Map([CanBeNull] DatabaseEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var indices = Tuple.Create(entity.StartIndex, entity.EndIndex);
            switch (entity.EntityType)
            {
                case EntityType.Media:
                    return new TwitterMediaEntity(indices, entity.UserId ?? 0,
                        entity.MediaUrl, entity.MediaUrl, entity.OriginalUrl, entity.DisplayText, entity.OriginalUrl,
                        MediaType.Unknown, null, null);
                case EntityType.Urls:
                    return new TwitterUrlEntity(indices, entity.OriginalUrl, entity.DisplayText, entity.OriginalUrl);
                case EntityType.UserMentions:
                    return new TwitterUserMentionEntity(indices, entity.UserId ?? 0, entity.DisplayText,
                        entity.DisplayText);
                case EntityType.Hashtags:
                    return new TwitterHashtagEntity(indices, entity.DisplayText);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TwitterStatus Map([CanBeNull] DatabaseStatus status, [CanBeNull] StatusEnts statusEntities,
            [CanBeNull] IEnumerable<long> favorers, [CanBeNull] IEnumerable<long> retweeters,
            [CanBeNull] TwitterUser user)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (statusEntities == null) throw new ArgumentNullException(nameof(statusEntities));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (status.StatusType != StatusType.Tweet)
            {
                throw new ArgumentException("This overload targeting normal tweet.");
            }
            if (status.UserId != user.Id)
            {
                throw new ArgumentException("ID mismatched between status and user.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            Tuple<int, int> displayTextRange = null;
            if (status.DisplayTextRangeBegin != null && status.DisplayTextRangeEnd != null)
            {
                displayTextRange = Tuple.Create(status.DisplayTextRangeBegin.Value, status.DisplayTextRangeEnd.Value);
            }
            Tuple<double, double> coords = null;
            if (status.Latitude != null && status.Longitude != null)
            {
                coords = Tuple.Create(status.Latitude.Value, status.Longitude.Value);
            }
            var favs = favorers?.ToArray() ?? new long[0];
            var rts = retweeters?.ToArray() ?? new long[0];
            return new TwitterStatus(status.Id, user, status.Text, displayTextRange, status.CreatedAt,
                ent.Select(Map).ToArray(), status.Source, status.InReplyToStatusId, status.InReplyToOrRecipientUserId,
                favs.Length, rts.Length, status.InReplyToOrRecipientScreenName, coords, null, null);
        }

        public static TwitterStatus Map([CanBeNull] DatabaseStatus status, [CanBeNull] StatusEnts statusEntities,
            [CanBeNull] IEnumerable<long> favorers, [CanBeNull] IEnumerable<long> retweeters,
            [CanBeNull] TwitterStatus originalStatus, bool isQuoted, [CanBeNull] TwitterUser user)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (statusEntities == null) throw new ArgumentNullException(nameof(statusEntities));
            if (originalStatus == null) throw new ArgumentNullException(nameof(originalStatus));
            if (user == null) throw new ArgumentNullException(nameof(user));

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
            Tuple<int, int> displayTextRange = null;
            if (status.DisplayTextRangeBegin != null && status.DisplayTextRangeEnd != null)
            {
                displayTextRange = Tuple.Create(status.DisplayTextRangeBegin.Value, status.DisplayTextRangeEnd.Value);
            }
            Tuple<double, double> coords = null;
            if (status.Latitude != null && status.Longitude != null)
            {
                coords = Tuple.Create(status.Latitude.Value, status.Longitude.Value);
            }
            var favs = favorers?.ToArray() ?? new long[0];
            var rts = retweeters?.ToArray() ?? new long[0];
            var retweet = isQuoted ? null : originalStatus;
            var quote = isQuoted ? originalStatus : null;
            return new TwitterStatus(status.Id, user, status.Text, displayTextRange, status.CreatedAt,
                ent.Select(Map).ToArray(), status.Source, status.InReplyToStatusId, status.InReplyToOrRecipientUserId,
                favs.Length, rts.Length, status.InReplyToOrRecipientScreenName, coords, retweet, quote);
        }


        public static TwitterStatus Map([CanBeNull] DatabaseStatus status, [CanBeNull] StatusEnts statusEntities,
            [CanBeNull] IEnumerable<long> favorers, [CanBeNull] IEnumerable<long> retweeters,
            [CanBeNull] TwitterStatus retweet, [CanBeNull] TwitterStatus quote,
            [CanBeNull] TwitterUser user)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (statusEntities == null) throw new ArgumentNullException(nameof(statusEntities));
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (retweet != null && status.RetweetOriginalId != retweet.Id)
            {
                throw new ArgumentException(
                    $"Retweet id is mismatched. parent id is {status.RetweetOriginalId}, but object has {retweet.Id}");
            }
            if (quote != null && status.QuoteId != quote.Id)
            {
                throw new ArgumentException(
                    $"Quote id is mismatched. parent id is {status.QuoteId}, but object has {quote.Id}");
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
            Tuple<int, int> displayTextRange = null;
            if (status.DisplayTextRangeBegin != null && status.DisplayTextRangeEnd != null)
            {
                displayTextRange = Tuple.Create(status.DisplayTextRangeBegin.Value, status.DisplayTextRangeEnd.Value);
            }
            Tuple<double, double> coords = null;
            if (status.Latitude != null && status.Longitude != null)
            {
                coords = Tuple.Create(status.Latitude.Value, status.Longitude.Value);
            }
            var favs = favorers?.ToArray() ?? new long[0];
            var rts = retweeters?.ToArray() ?? new long[0];
            return new TwitterStatus(status.Id, user, status.Text, displayTextRange, status.CreatedAt,
                ent.Select(Map).ToArray(), status.Source, status.InReplyToStatusId, status.InReplyToOrRecipientUserId,
                favs.Length, rts.Length, status.InReplyToOrRecipientScreenName, coords, retweet, quote);
        }


        public static TwitterStatus Map([CanBeNull] DatabaseStatus status, [CanBeNull] StatusEnts statusEntities,
            [CanBeNull] TwitterUser sender, [CanBeNull] TwitterUser recipient)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (statusEntities == null) throw new ArgumentNullException(nameof(statusEntities));
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (status.StatusType != StatusType.DirectMessage)
            {
                throw new ArgumentException("This overload targeting direct message.");
            }
            var ent = statusEntities.Memoize();
            if (ent.Any(e => e.ParentId != status.Id))
            {
                throw new ArgumentException("ID mismatched between status and entities.");
            }
            Tuple<int, int> displayTextRange = null;
            if (status.DisplayTextRangeBegin != null && status.DisplayTextRangeEnd != null)
            {
                displayTextRange = Tuple.Create(status.DisplayTextRangeBegin.Value, status.DisplayTextRangeEnd.Value);
            }

            return new TwitterStatus(status.Id, sender, recipient, status.Text, displayTextRange, status.CreatedAt,
                ent.Select(Map).ToArray());
        }

        #endregion map to object model

        #region map to object model(many)

        public static IEnumerable<TwitterUser> MapMany([CanBeNull] IEnumerable<DatabaseUser> users,
            [CanBeNull] Dictionary<long, UserDescEnts> dedic, [CanBeNull] Dictionary<long, UserUrlEnts> uedic)
        {
            if (users == null) throw new ArgumentNullException(nameof(users));
            if (dedic == null) throw new ArgumentNullException(nameof(dedic));
            if (uedic == null) throw new ArgumentNullException(nameof(uedic));
            return users.Select(user => Map(user, Resolve(dedic, user.Id), Resolve(uedic, user.Id)));
        }

        public static IEnumerable<T> Resolve<T>(IDictionary<long, IEnumerable<T>> dictionary, long id)
        {
            IEnumerable<T> value;
            return dictionary.TryGetValue(id, out value) ? value : Enumerable.Empty<T>();
        }

        #endregion map to object model(many)
    }
}