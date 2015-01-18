using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Tweets
    {
        #region statuses/show

        public static Task<long?> GetMyRetweetIdOfStatusAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetMyRetweetIdOfStatusAsync(id, CancellationToken.None);
        }


        public static async Task<long?> GetMyRetweetIdOfStatusAsync(
            [NotNull] this IOAuthCredential credential, long id,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"include_my_retweet", true}
            };
            var respStr = await credential.GetStringAsync("statuses/show.json", param, cancellationToken);
            return await Task.Run(() =>
            {
                var graph = DynamicJson.Parse(respStr);
                return ((bool)graph.current_user_retweet())
                    ? Int64.Parse(graph.current_user_retweet.id_str)
                    : null;
            }, cancellationToken);
        }

        #endregion

        #region statuses/retweets/:id

        public static Task<IEnumerable<TwitterUser>> GetRetweetsAsync(
            [NotNull] this IOAuthCredential credential, long id, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetRetweetsAsync(id, count, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterUser>> GetRetweetsAsync(
            [NotNull] this IOAuthCredential credential, long id, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            /*
            for future compatibility
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"count", count},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = client.GetAsync(new ApiAccess("statuses/retweets.json", param)
            */
            var param = new Dictionary<string, object> { { "count", count } };
            var response = await credential.GetAsync("statuses/retweets/" + id + ".json",
                param, cancellationToken);
            return await response.ReadAsUserCollectionAsync();
        }

        #endregion

        #region retweeter/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetRetweeterIdsAsync(
            this IOAuthCredential credential, long id, long cursor = -1)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetRetweeterIdsAsync(id, cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetRetweeterIdsAsync(
            this IOAuthCredential credential, long id, long cursor,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"cursor", cursor}
            };
            var response = await credential.GetAsync("retweeters/ids.json", param, cancellationToken);
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region statuses/show

        public static Task<TwitterStatus> ShowTweetAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.ShowTweetAsync(id, CancellationToken.None);
        }


        public static async Task<TwitterStatus> ShowTweetAsync(
            [NotNull] this IOAuthCredential credential, long id,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
            };
            var response = await credential.GetAsync("statuses/show.json", param, cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion

        #region statuses/update

        public static Task<TwitterStatus> UpdateAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] StatusParameter status)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            return credential.UpdateAsync(status, CancellationToken.None);
        }

        public static async Task<TwitterStatus> UpdateAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] StatusParameter status,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            var response = await credential.PostAsync("statuses/update.json",
                status.ToDictionary(), cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion

        #region statuses/update_with_media

        [Obsolete]
        public static Task<TwitterStatus> UpdateWithMediaAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string status, [NotNull] IEnumerable<byte[]> images,
            bool? possiblySensitive = false, long? inReplyToStatusId = null,
            [CanBeNull] Tuple<double, double> geoLatLong = null, string placeId = null, bool? displayCoordinates = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            if (images == null) throw new ArgumentNullException("images");
            return credential.UpdateWithMediaAsync(status, images, possiblySensitive, inReplyToStatusId, geoLatLong,
                placeId, displayCoordinates, CancellationToken.None);
        }

        [Obsolete]
        public static async Task<TwitterStatus> UpdateWithMediaAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string status, [NotNull] IEnumerable<byte[]> images,
            bool? possiblySensitive, long? inReplyToStatusId, [CanBeNull] Tuple<double, double> geoLatLong,
            [CanBeNull] string placeId, bool? displayCoordinates, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            if (images == null) throw new ArgumentNullException("images");
            var param = new Dictionary<string, object>
            {
                {"status", status},
                {"possibly_sensitive", possiblySensitive},
                {"in_reply_to_status_id", inReplyToStatusId},
                {"lat", geoLatLong != null ? geoLatLong.Item1 : (double?) null},
                {"long", geoLatLong != null ? geoLatLong.Item2 : (double?) null},
                {"place_id", placeId},
                {"display_coordinates", displayCoordinates}
            }.Where(kvp => kvp.Value != null);
            var content = new MultipartFormDataContent();
            param.ForEach(kvp => content.Add(new StringContent(kvp.Value.ToString()), kvp.Key));
            images.ForEach((b, i) => content.Add(new ByteArrayContent(b), "media[]", "image_" + i + ".png"));
            var response = await credential.PostAsync("statuses/update_with_media.json", content, cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion

        #region statuses/update_with_media2(media/upload)

        public static Task<TwitterStatus> UpdateWithMedia2Async(
            [NotNull] this IOAuthCredential credential, [NotNull] StatusParameter status,
           [NotNull] IEnumerable<byte[]> images)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            if (images == null) throw new ArgumentNullException("images");
            return credential.UpdateWithMedia2Async(status, images, CancellationToken.None);
        }

        public static async Task<TwitterStatus> UpdateWithMedia2Async(
            [NotNull]this IOAuthCredential credential, [NotNull] StatusParameter status,
            [NotNull] IEnumerable<byte[]> images, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            if (images == null) throw new ArgumentNullException("images");
            var ids = new List<long>();
            foreach (var image in images)
            {
                ids.Add(await credential.UploadMediaAsync(image, cancellationToken));
            }
            status.MediaIds = ids.ToArray();
            return await credential.UpdateAsync(status, cancellationToken);
        }

        public static async Task<long> UploadMediaAsync([NotNull] this IOAuthCredential credential,
           [NotNull] byte[] image, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new ByteArrayContent(image), "media", System.IO.Path.GetRandomFileName() + ".png"}
            };

            var client = credential.CreateOAuthClient();
            var resp = await client.PostAsync(HttpUtility.ConcatUrl(
                ApiAccessProperties.UploadEndpoint, "media/upload.json"),
                content, cancellationToken);
            var json = await resp.ReadAsStringAsync();
            return await Task.Run(() => long.Parse(DynamicJson.Parse(json).media_id_string),
                cancellationToken);
        }

        #endregion

        #region statuses/destroy/:id

        public static Task<TwitterStatus> DestroyAsync([NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.DestroyAsync(id, CancellationToken.None);
        }


        public static async Task<TwitterStatus> DestroyAsync([NotNull] this IOAuthCredential credential, long id,
           CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var response = await credential.PostAsync(("statuses/destroy/" + id + ".json"),
                new Dictionary<string, object>(), cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion

        #region statuses/retweet/:id

        public static Task<TwitterStatus> RetweetAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.RetweetAsync(id, CancellationToken.None);
        }

        public static async Task<TwitterStatus> RetweetAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var response = await credential.PostAsync("statuses/retweet/" + id + ".json",
                new Dictionary<string, object>(), cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion
    }
}
