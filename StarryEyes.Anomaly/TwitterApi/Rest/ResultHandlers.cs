using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class ResultHandlers
    {
        public static async Task<TwitterUser> ReadAsUserAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return new TwitterUser(DynamicJson.Parse(json));
        }

        public static async Task<TwitterStatus> ReadAsStatusAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            return new TwitterStatus(DynamicJson.Parse(json));
        }

        public static async Task<IEnumerable<TwitterStatus>> ReadAsStatusCollectionAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadAsStringAsync();
            var parsed = DynamicJson.Parse(json);
            // temporarily implementation
            var converteds = new List<TwitterStatus>();
            foreach (var status in parsed)
            {
                converteds.Add(new TwitterStatus(status));
            }
            return converteds;
        }

        public static async Task<string> ReadAsStringAsync(this HttpResponseMessage response)
        {
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }
    }
}
