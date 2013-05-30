using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Timelines
    {
        public static async Task<IEnumerable<TwitterStatus>> GetHomeTimeline(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
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

        public static Task<IEnumerable<TwitterStatus>> GetUserTimeline(
            this IOAuthCredential credential, long userId,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null)
        {
            return GetUserTimelineCore(credential, userId, null, count, sinceId, maxId, excludeReplies);
        }

        public static Task<IEnumerable<TwitterStatus>> GetUserTimeline(
            this IOAuthCredential credential, string screenName,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null)
        {
            return GetUserTimelineCore(credential, null, screenName, count, sinceId, maxId, excludeReplies);
        }

        private static async Task<IEnumerable<TwitterStatus>> GetUserTimelineCore(
            this IOAuthCredential credential, long? userId, string screenName,
            int? count, long? sinceId, long? maxId, bool? excludeReplies)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"exclude_replies", excludeReplies},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/user_timeline.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }


    }
}
