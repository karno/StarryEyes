using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Tweets
    {
        public static async Task<long?> GetMyRetweetIdOfStatusAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"include_my_retweet", true}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var respStr = await client.GetStringAsync(new ApiAccess("statuses/show.json", param));
            return await Task.Run(() =>
            {
                var graph = DynamicJson.Parse(respStr);
                return ((bool)graph.current_user_retweet())
                    ? Int64.Parse(graph.current_user_retweet.id_str)
                    : null;
            });
        }

        public static async Task<IEnumerable<TwitterUser>> GetRetweetsAsync(
            this IOAuthCredential credential, long id, int? count = null)
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
            var response = client.GetAsync(new ApiAccess("statuses/retweets.json", param))
            */
            var param = new Dictionary<string, object>
            {
                {"count", count},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/retweets/" + id + ".json", param));
            return await response.ReadAsUserCollectionAsync();
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetRetweeterIdsAsync(
            this IOAuthCredential credential, long id, long cursor = -1)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"cursor", cursor},
                {"stringify_ids", "true"}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("retweeters/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        public static async Task<TwitterStatus> ShowTweetAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("statuses/show.json", param));
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> UpdateAsync(
            this IOAuthCredential credential, string status, long? inReplyToStatusId = null,
            Tuple<double, double> geoLatLong = null, string placeId = null,
            bool? displayCoordinates = null, long[] mediaIds = null, bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            var mediaIdStr = mediaIds != null
                ? String.Join(",", mediaIds.Select(s => s.ToString()))
                : null;
            var param = new Dictionary<string, object>
            {
                {"status", status},
                {"in_reply_to_status_id", inReplyToStatusId},
                {"lat", geoLatLong != null ? geoLatLong.Item1 : (double?) null},
                {"long", geoLatLong != null ? geoLatLong.Item2 : (double?) null},
                {"place_id", placeId},
                {"display_coordinates", displayCoordinates},
                {"media_ids", String.IsNullOrEmpty(mediaIdStr) ? null : mediaIdStr},
                {"tweet_mode", extendedTweet ? "extended" : null }
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient(useGZip: false);
            var response = await client.PostAsync(new ApiAccess("statuses/update.json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<long> UploadMediaAsync(this IOAuthCredential credential, byte[] image)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new ByteArrayContent(image), "media", System.IO.Path.GetRandomFileName() + ".png"}
            };

            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync("https://upload.twitter.com/1.1/media/upload.json", content);
            var json = await response.ReadAsStringAsync();
            return long.Parse(DynamicJson.Parse(json).media_id_string);
        }

        public static async Task<TwitterStatus> DestroyAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>().ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("statuses/destroy/" + id + ".json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> RetweetAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>().ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("statuses/retweet/" + id + ".json"), param);
            return await response.ReadAsStatusAsync();
        }


    }
}
