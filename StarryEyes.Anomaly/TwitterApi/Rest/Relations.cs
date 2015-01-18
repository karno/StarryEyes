using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Relations
    {
        #region friends/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetFriendsIdsAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter nullableTargetUser,
            long? cursor = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetFriendsIdsAsync(nullableTargetUser, cursor, count, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetFriendsIdsAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter nullableTargetUser,
            long? cursor, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor},
                {"count", count}
            }.ApplyParameter(nullableTargetUser);
            var response = await credential.GetAsync("friends/ids.json", param, cancellationToken);
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region followers/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetFollowersIdsAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter nullableTargetUser,
            long? cursor = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFollowersIdsAsync(credential, nullableTargetUser, cursor, count, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetFollowersIdsAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter nullableTargetUser,
            long? cursor, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor},
                {"count", count}
            }.ApplyParameter(nullableTargetUser);
            var response = await credential.GetAsync("followers/ids.json", param, cancellationToken);
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region friendships/no_retweets/ids

        public static Task<IEnumerable<long>> GetNoRetweetsIdsAsync(
            [NotNull] this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetNoRetweetsIdsAsync(CancellationToken.None);
        }

        public static async Task<IEnumerable<long>> GetNoRetweetsIdsAsync(
            [NotNull] this IOAuthCredential credential, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var respStr = await credential.GetStringAsync("friendships/no_retweets/ids.json",
                new Dictionary<string, object>(), cancellationToken);
            return await Task.Run(() => ((dynamic[])DynamicJson.Parse(respStr))
                .Select(d => (long)d), cancellationToken);
        }

        #endregion

        #region mutes/users/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetMuteIdsAsync(
            [NotNull] this IOAuthCredential credential, long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetMuteIdsAsync(cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetMuteIdsAsync(
            [NotNull] this IOAuthCredential credential, long? cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor}
            };
            var resp = await credential.GetAsync("mutes/users/ids.json",
                param, cancellationToken);
            return await resp.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region friendships/create

        public static Task<TwitterUser> CreateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.CreateFriendshipAsync(targetUser);
        }

        public static async Task<TwitterUser> CreateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var response = await credential.PostAsync("friendships/create.json",
                targetUser.ToDictionary(), cancellationToken);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region friendships/destroy

        public static Task<TwitterUser> DestroyFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.DestroyFriendshipAsync(targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> DestroyFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var response = await credential.PostAsync("friendships/destroy.json",
                targetUser.ToDictionary(), cancellationToken);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region friendships/show

        public static Task<TwitterFriendship> ShowFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter sourceUser,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (sourceUser == null) throw new ArgumentNullException("sourceUser");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.ShowFriendshipAsync(sourceUser, targetUser, CancellationToken.None);
        }

        public static async Task<TwitterFriendship> ShowFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter sourceUser,
            [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (sourceUser == null) throw new ArgumentNullException("sourceUser");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            sourceUser.SetKeyAsSource();
            targetUser.SetKeyAsTarget();
            var param = sourceUser.ToDictionary().ApplyParameter(targetUser);
            var response = await credential.GetAsync("friendships/show.json", param, cancellationToken);
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

        #region friendships/update

        public static Task<TwitterFriendship> UpdateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter screenName,
            bool? enableDeviceNotifications, bool? showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");

            return credential.UpdateFriendshipAsync(screenName, enableDeviceNotifications, showRetweet,
                CancellationToken.None);
        }

        public static async Task<TwitterFriendship> UpdateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter screenName,
            bool? enableDeviceNotifications, bool? showRetweet, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");

            var param = new Dictionary<string, object>
            {
                {"device", enableDeviceNotifications},
                {"retweets", showRetweet},
            }.ApplyParameter(screenName);
            var response = await credential.PostAsync("friendships/update.json", param,
                cancellationToken);
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

        #region mutes/users/[create|destroy]

        public static Task<TwitterUser> UpdateMuteAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser, bool mute)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.UpdateMuteAsync(targetUser, mute, CancellationToken.None);
        }

        public static async Task<TwitterUser> UpdateMuteAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser, bool mute,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var endpoint = mute ? "mutes/users/create" : "mutes/users/destroy";
            var response = await credential.PostAsync(endpoint,
                targetUser.ToDictionary(), cancellationToken);
            return await response.ReadAsUserAsync();
        }

        #endregion
    }
}

