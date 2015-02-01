using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Tweets
    {
        #region statuses/show

        public static Task<long?> GetMyRetweetIdOfStatusAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetMyRetweetIdOfStatusAsync(properties, id, CancellationToken.None);
        }


        public static async Task<long?> GetMyRetweetIdOfStatusAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"include_my_retweet", true}
            };
            return await credential.GetAsync(properties, "statuses/show.json", param, async resp =>
            {
                var json = await resp.ReadAsStringAsync().ConfigureAwait(false);
                var graph = DynamicJson.Parse(json);
                return ((bool)graph.current_user_retweet()) ? Int64.Parse(graph.current_user_retweet.id_str) : null;
            }, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/retweets/:id

        public static Task<IEnumerable<TwitterUser>> GetRetweetsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetRetweetsAsync(properties, id, count, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterUser>> GetRetweetsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object> { { "count", count } };
            return await credential.GetAsync(properties, "statuses/retweets/" + id + ".json", param,
                ResultHandlers.ReadAsUserCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region retweeter/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetRetweeterIdsAsync(
            this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, long cursor = -1)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetRetweeterIdsAsync(properties, id, cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetRetweeterIdsAsync(
            this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, long cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"cursor", cursor}
            };
            return await credential.GetAsync(properties, "retweeters/ids.json", param,
                ResultHandlers.ReadAsCursoredIdsAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/show

        public static Task<TwitterStatus> ShowTweetAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.ShowTweetAsync(properties, id, CancellationToken.None);
        }


        public static async Task<TwitterStatus> ShowTweetAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id},
            };
            return await credential.GetAsync(properties, "statuses/show.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/update

        public static Task<TwitterStatus> UpdateAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] StatusParameter status)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (status == null) throw new ArgumentNullException("status");
            return credential.UpdateAsync(properties, status, CancellationToken.None);
        }

        public static async Task<TwitterStatus> UpdateAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] StatusParameter status, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (status == null) throw new ArgumentNullException("status");
            return await credential.PostAsync(properties, "statuses/update.json", status.ToDictionary(),
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region media/upload

        public static Task<long> UploadMediaAsync([NotNull] this IOAuthCredential credential,
            [NotNull] IApiAccessProperties properties, [NotNull] byte[] image)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (image == null) throw new ArgumentNullException("image");
            return credential.UploadMediaAsync(properties, image, CancellationToken.None);
        }


        public static async Task<long> UploadMediaAsync([NotNull] this IOAuthCredential credential,
            [NotNull] IApiAccessProperties properties, [NotNull] byte[] image, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new ByteArrayContent(image), "media", System.IO.Path.GetRandomFileName() + ".png"}
            };

            return await credential.PostAsync(properties, "media/upload.json", content, async resp =>
            {
                var json = await resp.ReadAsStringAsync().ConfigureAwait(false);
                return long.Parse(DynamicJson.Parse(json).media_id_string);
            }, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region statuses/destroy/:id

        public static Task<TwitterStatus> DestroyAsync([NotNull] this IOAuthCredential credential,
            [NotNull] IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.DestroyAsync(properties, id, CancellationToken.None);
        }


        public static async Task<TwitterStatus> DestroyAsync([NotNull] this IOAuthCredential credential,
            [NotNull] IApiAccessProperties properties, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.PostAsync(properties, "statuses/destroy/" + id + ".json",
                new Dictionary<string, object>(), ResultHandlers.ReadAsStatusAsync, cancellationToken)
                                   .ConfigureAwait(false);
        }

        #endregion

        #region statuses/retweet/:id

        public static Task<TwitterStatus> RetweetAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.RetweetAsync(properties, id, CancellationToken.None);
        }

        public static async Task<TwitterStatus> RetweetAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.PostAsync(properties, "statuses/retweet/" + id + ".json",
                new Dictionary<string, object>(), ResultHandlers.ReadAsStatusAsync, cancellationToken)
                                   .ConfigureAwait(false);
        }

        #endregion
    }
}

