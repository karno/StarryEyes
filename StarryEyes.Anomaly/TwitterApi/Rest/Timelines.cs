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
    public static class Timelines
    {
        #region statuses/home_timeline

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetHomeTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetHomeTimelineAsync(properties, count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetHomeTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count, long? sinceId, long? maxId, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId}
            };
            return await credential.GetAsync(properties, "statuses/home_timeline.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/user_timeline

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetUserTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, int? count = null, long? sinceId = null, long? maxId = null,
            bool? excludeReplies = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.GetUserTimelineAsync(properties, targetUser,
                count, sinceId, maxId, excludeReplies, includeRetweets,
                CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetUserTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, int? count, long? sinceId, long? maxId,
            bool? excludeReplies, bool? includeRetweets, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var param = new Dictionary<string, object>
            {
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"exclude_replies", excludeReplies},
                {"include_rts", includeRetweets},
            }.ApplyParameter(targetUser);
            return await credential.GetAsync(properties, "statuses/user_timeline.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/mentions_timeline

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetMentionsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count = null, long? sinceId = null, long? maxId = null, bool? includeRetweets = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetMentionsAsync(properties, count, sinceId, maxId,
                includeRetweets, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetMentionsAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            int? count, long? sinceId, long? maxId, bool? includeRetweets,
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
            return await credential.GetAsync(properties, "statuses/mentions_timeline.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/retweets_of_me

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetRetweetsOfMeAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            int? count = null, long? sinceId = null, long? maxId = null)
        {

            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetRetweetsOfMeAsync(properties, count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetRetweetsOfMeAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count, long? sinceId, long? maxId, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            };
            return await credential.GetAsync(properties, "statuses/retweets_of_me.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
