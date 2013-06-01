using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Users
    {
        public static Task<IEnumerable<TwitterUser>> LookupUser(
            this IOAuthCredential credential, IEnumerable<long> userIds)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (userIds == null) throw new ArgumentNullException("userIds");
            return LookupUserCore(credential, userIds, null);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUser(
            this IOAuthCredential credential, IEnumerable<string> screenNames)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenNames == null) throw new ArgumentNullException("screenNames");
            return LookupUserCore(credential, null, screenNames);
        }

        private static async Task<IEnumerable<TwitterUser>> LookupUserCore(
            IOAuthCredential credential, IEnumerable<long> userIds, IEnumerable<string> screenNames)
        {
            var param = new Dictionary<string, object>
            {
                {
                    "user_id",
                    userIds == null
                        ? null
                        : userIds.Select(s => s.ToString(CultureInfo.InvariantCulture))
                                 .JoinString(",")
                },
                {"screen_name", screenNames == null ? null : screenNames.JoinString(",")},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("users/lookup.json", param));
            return await response.ReadAsUserCollectionAsync();
        }

        public static async Task<IEnumerable<TwitterUser>> SearchUser(
            this IOAuthCredential credential, string query, int? page = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"q", query},
                {"page", page},
                {"count", count},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("users/search.json", param));
            return await response.ReadAsUserCollectionAsync();
        }

        public static Task<TwitterUser> ShowUser(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ShowUserCore(credential, userId, null);
        }

        public static Task<TwitterUser> ShowUser(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return ShowUserCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> ShowUserCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("users/show.json", param));
            return await response.ReadAsUserAsync();
        }
    }
}
