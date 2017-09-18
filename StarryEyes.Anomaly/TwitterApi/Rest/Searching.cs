using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Searching
    {
        public static async Task<IEnumerable<TwitterStatus>> SearchAsync(
            this IOAuthCredential credential, string query,
            string geoCode = null, string lang = null, string locale = null,
            SearchResultType resultType = SearchResultType.Mixed,
            int? count = null, DateTime? untilDate = null,
            long? sinceId = null, long? maxId = null, bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"q", query},
                {"geocode", geoCode},
                {"lang", lang},
                {"locale", locale},
                {"result_type", resultType.ToString().ToLower()},
                {"count", count},
                {"until", untilDate != null ? untilDate.Value.ToString("yyyy-MM-dd") : null},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"tweet_mode", extendedTweet ? "extended" : null }
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("search/tweets.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<IEnumerable<Tuple<long, string>>> GetSavedSearchesAsync(
            this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var client = credential.CreateOAuthClient();
            var respStr = await client.GetStringAsync(new ApiAccess("saved_searches/list.json"));
            return await Task.Run(() =>
            {
                var parsed = DynamicJson.Parse(respStr);
                return (((dynamic[])parsed).Select(
                    item => Tuple.Create(Int64.Parse((string)item.id_str), (string)item.query)));
            });
        }

        public static async Task<Tuple<long, string>> SaveSearchAsync(
            this IOAuthCredential credential, string query)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"query", query}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("saved_searches/create.json"), param);
            var respStr = await response.ReadAsStringAsync();
            return await Task.Run(() =>
            {
                var json = DynamicJson.Parse(respStr);
                return Tuple.Create(Int64.Parse(json.id_str), json.query);
            });
        }

        public static async Task<Tuple<long, string>> DestroySavedSearchAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>().ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("saved_searches/destroy/" + id + ".json"), param);
            var respStr = await response.ReadAsStringAsync();
            return await Task.Run(() =>
            {
                var json = DynamicJson.Parse(respStr);
                return Tuple.Create(Int64.Parse(json.id_str), json.query);
            });
        }
    }

    public enum SearchResultType
    {
        Mixed,
        Recent,
        Popular,
    }
}
