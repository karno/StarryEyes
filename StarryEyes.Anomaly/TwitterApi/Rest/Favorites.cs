using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Favorites
    {
        public static Task<IEnumerable<TwitterStatus>> GetFavorites(
            this IOAuthCredential credential, long userId,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFavoritesCore(credential, userId, null, count, sinceId, maxId);
        }

        public static Task<IEnumerable<TwitterStatus>> GetFavorites(
            this IOAuthCredential credential, string screenName,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFavoritesCore(credential, null, screenName, count, sinceId, maxId);
        }

        private static async Task<IEnumerable<TwitterStatus>> GetFavoritesCore(
            IOAuthCredential credential, long? userId, string screenName,
            int? count, long? sinceId, long? maxId)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("favorites/list.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<TwitterStatus> CreateFavorite(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("favorites/create.json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> DestroyFavorite(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("favorites/destroy.json"), param);
            return await response.ReadAsStatusAsync();
        }
    }
}
