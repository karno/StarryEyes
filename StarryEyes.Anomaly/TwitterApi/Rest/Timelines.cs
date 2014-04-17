using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Timelines
    {
        public static async Task<IEnumerable<TwitterStatus>> GetHomeTimelineAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/home_timeline.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        #region User timeline
        public static Task<IEnumerable<TwitterStatus>> GetUserTimelineAsync(
            this IOAuthCredential credential, long userId,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetUserTimelineCoreAsync(credential, userId, null, count, sinceId, maxId, excludeReplies, includeRetweets);
        }

        public static Task<IEnumerable<TwitterStatus>> GetUserTimelineAsync(
            this IOAuthCredential credential, string screenName,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetUserTimelineCoreAsync(credential, null, screenName, count, sinceId, maxId, excludeReplies, includeRetweets);
        }

        private static async Task<IEnumerable<TwitterStatus>> GetUserTimelineCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            int? count, long? sinceId, long? maxId, bool? excludeReplies, bool? includeRetweets)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"exclude_replies", excludeReplies},
                {"include_rts", includeRetweets},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/user_timeline.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        public static async Task<IEnumerable<TwitterStatus>> GetMentionsAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"include_rts", includeRetweets},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/mentions_timeline.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<IEnumerable<TwitterStatus>> GetRetweetsOfMeAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/retweets_of_me.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }
    }
}
