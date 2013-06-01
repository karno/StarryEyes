using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Relations
    {
        public static async Task<IEnumerable<long>> GetNoRetweetsIds(
            this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var client = credential.CreateOAuthClient();
            var respStr = await client.GetStringAsync(new ApiAccess("friendships/no_retweets/ids.json"));
            return ((dynamic[])DynamicJson.Parse(respStr)).OfType<long>();
        }

        #region FriendsIds

        public static Task<CursorResult<IEnumerable<long>>> GetFriendsIds(
            this IOAuthCredential credential, long userId,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFriendsIdsCore(credential, userId, null, cursor, count);
        }

        public static Task<CursorResult<IEnumerable<long>>> GetFriendsIds(
            this IOAuthCredential credential, string screenName,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFriendsIdsCore(credential, null, screenName, cursor, count);
        }

        private static async Task<CursorResult<IEnumerable<long>>> GetFriendsIdsCore(
            IOAuthCredential credential, long? userId, string screenName,
            long cursor, int? count)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"cursor", cursor},
                {"count", count}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("friends/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region FollowersIds

        public static Task<CursorResult<IEnumerable<long>>> GetFollowersIds(
            this IOAuthCredential credential, long userId,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFollowersIdsCore(credential, userId, null, cursor, count);
        }

        public static Task<CursorResult<IEnumerable<long>>> GetFollowersIds(
            this IOAuthCredential credential, string screenName,
            long cursor = -1, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFollowersIdsCore(credential, null, screenName, cursor, count);
        }

        private static async Task<CursorResult<IEnumerable<long>>> GetFollowersIdsCore(
            IOAuthCredential credential, long? userId, string screenName,
            long cursor, int? count)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"cursor", cursor},
                {"count", count}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("followers/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region Follow

        public static Task<TwitterUser> CreateFriendship(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return CreateFriendshipCore(credential, userId, null);
        }

        public static Task<TwitterUser> CreateFriendship(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return CreateFriendshipCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> CreateFriendshipCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/create.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Remove

        public static Task<TwitterUser> DestroyFriendship(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return DestroyFriendshipCore(credential, userId, null);
        }

        public static Task<TwitterUser> DestroyFriendship(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return DestroyFriendshipCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> DestroyFriendshipCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/destroy.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Show friendships

        public static Task<TwitterFriendship> ShowFriendship(
            this IOAuthCredential credential, long sourceId, long targetId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ShowFriendshipCore(credential, sourceId, null, targetId, null);
        }

        public static Task<TwitterFriendship> ShowFriendship(
            this IOAuthCredential credential, string sourceScreenName, string targetScreenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (sourceScreenName == null) throw new ArgumentNullException("sourceScreenName");
            if (targetScreenName == null) throw new ArgumentNullException("targetScreenName");
            return ShowFriendshipCore(credential, null, sourceScreenName, null, targetScreenName);
        }

        public static async Task<TwitterFriendship> ShowFriendshipCore(
            IOAuthCredential credential, long? sourceId, string sourceScreenName,
                long? targetId, string targetScreenName)
        {
            var param = new Dictionary<string, object>
            {
                {"source_id", sourceId},
                {"source_screen_name", sourceScreenName},
                {"target_id", targetId},
                {"target_screen_name", targetScreenName},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("friendships/show.json", param));
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

        #region Update friendships

        public static Task<TwitterFriendship> UpdateFriendship(
            this IOAuthCredential credential, long userId, bool showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return UpdateFriendshipCore(credential, userId, null, showRetweet);
        }

        public static Task<TwitterFriendship> UpdateFriendship(
            this IOAuthCredential credential, string screenName, bool showRetweet)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return UpdateFriendshipCore(credential, null, screenName, showRetweet);
        }

        private static async Task<TwitterFriendship> UpdateFriendshipCore(
            IOAuthCredential credential, long? userId, string screenName, bool showRetweet)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"retweets", showRetweet},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("friendships/update.json"), param);
            return await response.ReadAsFriendshipAsync();
        }

        #endregion

    }
}
