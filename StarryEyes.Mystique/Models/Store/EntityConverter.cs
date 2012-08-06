using System;
using System.Linq;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Store
{
    public static class EntityConverter
    {
        public static Status ToDbStatus(this TwitterStatus status)
        {
            var dbstatus = new Status()
            {
                Id = status.Id,
                StatusType = (int)status.StatusType,
                IsDataLacking = status.IsDataLacking,
                User = status.User.ToDbUser(),
                Text = status.Text,
                CreatedAt = status.CreatedAt,
                IsFavored = status.IsFavored,
                Source = status.Source,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToScreenName = status.InReplyToScreenName,
                RetweetedOriginal = (status.RetweetedOriginal != null ? status.RetweetedOriginal.ToDbStatus() : null),
                Longitude = (float)status.Longitude,
                Latitude = (float)status.Latitude,
                Recipient = (status.Recipient != null ? status.Recipient.ToDbUser() : null),
            };
            status.Entities
                .Select(ent => ent.ToDbEntity(status))
                .ForEach(e => dbstatus.Entities.Add(e));
            return dbstatus;
        }

        public static TwitterStatus ToTwitterStatus(this Status status)
        {
            return new TwitterStatus()
            {
                Id = status.Id,
                StatusType = (StatusType)status.StatusType,
                IsDataLacking = status.IsDataLacking,
                User = status.User.ToTwitterUser(),
                Text = status.Text,
                CreatedAt = status.CreatedAt,
                IsFavored = status.IsFavored,
                Source = status.Source,
                InReplyToStatusId = status.InReplyToStatusId,
                InReplyToScreenName = status.InReplyToScreenName,
                RetweetedOriginal = (status.RetweetedOriginal != null ? status.RetweetedOriginal.ToTwitterStatus() : null),
                Latitude = status.Latitude,
                Longitude = status.Longitude,
                Recipient = (status.Recipient != null ? status.Recipient.ToTwitterUser() : null),
            };
        }

        public static User ToDbUser(this TwitterUser user)
        {
            return new User()
            {
                Id = user.Id,
                ScreenName = user.ScreenName,
                IsDataLacking = user.IsDataLacking,
                Name = user.Name,
                Description = user.Description,
                Location = user.Location,
                Url = user.Url,
                ProfileImageUri = user.ProfileImageUri.OriginalString,
                IsProtected = user.IsProtected,
                IsVerified = user.IsVerified,
                IsTranslator = user.IsTranslator,
                IsContributorsEnabled = user.IsContributorsEnabled,
                IsGeoEnabled = user.IsGeoEnabled,
                StatusesCount = user.StatusesCount,
                FriendsCount = user.FriendsCount,
                FollowersCount = user.FollowersCount,
                FavoritesCount = user.FavoritesCount,
                ListedCount = user.ListedCount,
                Language = user.Language,
                CreatedAt = user.CreatedAt,
            };
        }

        public static TwitterUser ToTwitterUser(this User user)
        {
            return new TwitterUser()
            {
                Id = user.Id,
                ScreenName = user.ScreenName,
                IsDataLacking = user.IsDataLacking,
                Name = user.Name,
                Description = user.Description,
                Location = user.Location,
                Url = user.Url,
                ProfileImageUri = new Uri(user.ProfileImageUri),
                IsProtected = user.IsProtected,
                IsVerified = user.IsVerified,
                IsTranslator = user.IsTranslator,
                IsContributorsEnabled = user.IsContributorsEnabled,
                IsGeoEnabled = user.IsGeoEnabled,
                StatusesCount = user.StatusesCount,
                FriendsCount = user.FriendsCount,
                FollowersCount = user.FollowersCount,
                FavoritesCount = user.FavoritesCount,
                ListedCount = user.ListedCount,
                Language = user.Language,
                CreatedAt = user.CreatedAt,
            };
        }

        public static Entity ToDbEntity(this TwitterEntity entity, TwitterStatus status)
        {
            return new Entity()
            {
                Id = entity.InternalId,
                StatusId = status.Id,
                EntityType = (int)entity.EntityType,
                DisplayText = entity.DisplayText,
                OriginalText = entity.OriginalText,
                MediaUrl = entity.MediaUrl,
                StartIndex = entity.StartIndex,
                EndIndex = entity.EndIndex,
            };
        }

        public static TwitterEntity ToTwitterEntity(this Entity entity)
        {
            return new TwitterEntity()
            {
                InternalId = entity.Id,
                EntityType = (EntityType)entity.EntityType,
                DisplayText = entity.DisplayText,
                OriginalText = entity.OriginalText,
                MediaUrl = entity.MediaUrl,
                StartIndex = entity.StartIndex,
                EndIndex = entity.EndIndex,
            };
        }

        public static Activity ToDbActivity(this TwitterActivity activity)
        {
            return new Activity()
            {
                Id = activity.InternalId,
                CreatedAt = activity.CreatedAt,
                ActivityKind = (int)activity.Activity,
                SourceUser = activity.User.ToDbUser(),
                TargetUser = activity.TargetUser.ToDbUser(),
                TargetStatus = activity.TargetStatus.ToDbStatus(),
            };
        }

        public static TwitterActivity ToTwitterActivity(this Activity activity)
        {
            return new TwitterActivity()
            {
                InternalId = activity.Id,
                CreatedAt = activity.CreatedAt,
                Activity = (SweetLady.DataModel.Activity)activity.ActivityKind,
                User = activity.SourceUser.ToTwitterUser(),
                TargetUser = activity.TargetUser.ToTwitterUser(),
                TargetStatus = activity.TargetStatus.ToTwitterStatus(),
            };
        }
    }
}
