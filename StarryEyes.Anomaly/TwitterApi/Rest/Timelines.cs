using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Timelines
    {
        #region statuses/home_timeline

        public static Task<IEnumerable<TwitterStatus>> GetHomeTimelineAsync(
            [NotNull] this IOAuthCredential credential, int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetHomeTimelineAsync(count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetHomeTimelineAsync(
            [NotNull] this IOAuthCredential credential, int? count, long? sinceId, long? maxId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId}
            };
            var response = await credential.GetAsync("statuses/home_timeline.json",
                param, cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region statuses/user_timeline

        public static Task<IEnumerable<TwitterStatus>> GetUserTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return GetUserTimelineAsync(credential, targetUser,
                count, sinceId, maxId, excludeReplies, includeRetweets,
                CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetUserTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            int? count, long? sinceId, long? maxId, bool? excludeReplies, bool? includeRetweets,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var param = new Dictionary<string, object>
            {
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"exclude_replies", excludeReplies},
                {"include_rts", includeRetweets},
            }.ApplyParameter(targetUser);
            var response = await credential.GetAsync("statuses/user_timeline.json", param,
                cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region statuses/mentions_timeline

        public static Task<IEnumerable<TwitterStatus>> GetMentionsAsync(
            [NotNull] this IOAuthCredential credential, int? count = null,
            long? sinceId = null, long? maxId = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetMentionsAsync(count, sinceId, maxId,
                includeRetweets, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetMentionsAsync(
            [NotNull] this IOAuthCredential credential, int? count,
            long? sinceId, long? maxId, bool? includeRetweets,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"include_rts", includeRetweets},
            };
            var response = await credential.GetAsync("statuses/mentions_timeline.json", param,
                cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region statuses/retweets_of_me

        public static Task<IEnumerable<TwitterStatus>> GetRetweetsOfMeAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null)
        {

            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetRetweetsOfMeAsync(count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetRetweetsOfMeAsync(
            [NotNull] this IOAuthCredential credential, int? count, long? sinceId, long? maxId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            };
            var response = await credential.GetAsync("statuses/retweets_of_me.json", param,
                cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion
    }
}
