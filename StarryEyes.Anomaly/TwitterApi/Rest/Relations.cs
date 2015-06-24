using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Relations
    {
        #region friends/ids

        public static Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetFriendsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter nullableTargetUser, long? cursor = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetFriendsIdsAsync(properties, nullableTargetUser, cursor, count, CancellationToken.None);
        }

        public static async Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetFriendsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter nullableTargetUser, long? cursor, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor},
                {"count", count}
            }.ApplyParameter(nullableTargetUser);
            return await credential.GetAsync(properties, "friends/ids.json", param,
                ResultHandlers.ReadAsCursoredIdsAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region followers/ids

        public static Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetFollowersIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter nullableTargetUser, long? cursor = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetFollowersIdsAsync(properties, nullableTargetUser, cursor, count, CancellationToken.None);
        }

        public static async Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetFollowersIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter nullableTargetUser, long? cursor, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor},
                {"count", count}
            }.ApplyParameter(nullableTargetUser);
            return await credential.GetAsync(properties, "followers/ids.json", param,
                ResultHandlers.ReadAsCursoredIdsAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region friendships/no_retweets/ids

        public static Task<IApiResult<IEnumerable<long>>> GetNoRetweetsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetNoRetweetsIdsAsync(properties, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<long>>> GetNoRetweetsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.GetAsync(properties, "friendships/no_retweets/ids.json",
                new Dictionary<string, object>(), ResultHandlers.ReadAsIdCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region mutes/users/ids

        public static Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetMuteIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetMuteIdsAsync(properties, cursor, CancellationToken.None);
        }

        public static async Task<IApiResult<ICursorResult<IEnumerable<long>>>> GetMuteIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long? cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor}
            };
            return await credential.GetAsync(properties, "mutes/users/ids.json", param,
                ResultHandlers.ReadAsCursoredIdsAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region friendships/create

        public static Task<IApiResult<TwitterUser>> CreateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.CreateFriendshipAsync(properties, targetUser, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> CreateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return await credential.PostAsync(properties, "friendships/create.json", targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region friendships/destroy

        public static Task<IApiResult<TwitterUser>> DestroyFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.DestroyFriendshipAsync(properties, targetUser, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> DestroyFriendshipAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return await credential.PostAsync(properties, "friendships/destroy.json", targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region friendships/show

        public static Task<IApiResult<TwitterFriendship>> ShowFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter sourceUser, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (sourceUser == null) throw new ArgumentNullException("sourceUser");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.ShowFriendshipAsync(properties, sourceUser, targetUser, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterFriendship>> ShowFriendshipAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter sourceUser, [NotNull] UserParameter targetUser, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (sourceUser == null) throw new ArgumentNullException("sourceUser");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            sourceUser.SetKeyAsSource();
            targetUser.SetKeyAsTarget();
            var param = sourceUser.ToDictionary().ApplyParameter(targetUser);
            return await credential.GetAsync(properties, "friendships/show.json", param,
                ResultHandlers.ReadAsFriendshipAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region friendships/update

        public static Task<IApiResult<TwitterFriendship>> UpdateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter screenName, bool? enableDeviceNotifications, bool? showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (screenName == null) throw new ArgumentNullException("screenName");

            return credential.UpdateFriendshipAsync(properties, screenName, enableDeviceNotifications, showRetweet,
                CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterFriendship>> UpdateFriendshipAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter screenName, bool? enableDeviceNotifications, bool? showRetweet,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (screenName == null) throw new ArgumentNullException("screenName");

            var param = new Dictionary<string, object>
            {
                {"device", enableDeviceNotifications},
                {"retweets", showRetweet},
            }.ApplyParameter(screenName);
            return await credential.PostAsync(properties, "friendships/update.json", param,
                ResultHandlers.ReadAsFriendshipAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region mutes/users/[create|destroy]

        public static Task<IApiResult<TwitterUser>> UpdateMuteAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, bool mute)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.UpdateMuteAsync(properties, targetUser, mute, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> UpdateMuteAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, bool mute, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var endpoint = mute ? "mutes/users/create" : "mutes/users/destroy";
            return await credential.PostAsync(properties, endpoint, targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken);
        }

        #endregion
    }
}

