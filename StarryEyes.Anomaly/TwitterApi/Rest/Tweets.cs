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
        public static async Task<long?> GetMyRetweetIdOfStatus(
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
            var graph = DynamicJson.Parse(respStr);
            return ((bool)graph.current_user_retweet())
                       ? Int64.Parse(graph.current_user_retweet.id_str)
                       : null;
        }

        public static async Task<IEnumerable<TwitterUser>> GetRetweets(
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

        public static async Task<CursorResult<IEnumerable<long>>> GetRetweeterIds(
            this IOAuthCredential credential, long id, long cursor = -1)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"cursor", cursor}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("retweeters/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        public static async Task<TwitterStatus> ShowTweet(
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

        public static async Task<TwitterStatus> Update(
            this IOAuthCredential credential, string status, long? inReplyToStatusId = null,
            Tuple<double, double> geoLatLong = null, string placeId = null, bool? displayCoordinates = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
            var param = new Dictionary<string, object>
            {
                {"status", status},
                {"in_reply_to_status_id", inReplyToStatusId},
                {"lat", geoLatLong != null ? geoLatLong.Item1 : (double?) null},
                {"long", geoLatLong != null ? geoLatLong.Item2 : (double?) null},
                {"place_id", placeId},
                {"display_coordinates", displayCoordinates}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("statuses/update.json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> UpdateWithMedia(
            this IOAuthCredential credential, string status, IEnumerable<byte[]> images,
            bool? possiblySensitive = false, long? inReplyToStatusId = null,
            Tuple<double, double> geoLatLong = null, string placeId = null, bool? displayCoordinates = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (status == null) throw new ArgumentNullException("status");
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
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("statuses/update_with_media.json"), content);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> Destroy(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>().ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("statuses/destroy/" + id + ".json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> Retweet(
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
