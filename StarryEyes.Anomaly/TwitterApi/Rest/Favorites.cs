using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadena;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Favorites
    {
        public static Task<IEnumerable<TwitterStatus>> GetFavoritesAsync(
            this IOAuthCredential credential, long userId,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetFavoritesCoreAsync(credential, userId, null, count, sinceId, maxId, extendedTweet);
        }

        public static Task<IEnumerable<TwitterStatus>> GetFavoritesAsync(
            this IOAuthCredential credential, string screenName,
            int? count = null, long? sinceId = null, long? maxId = null,
            bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetFavoritesCoreAsync(credential, null, screenName, count, sinceId, maxId, extendedTweet);
        }

        private static async Task<IEnumerable<TwitterStatus>> GetFavoritesCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            int? count, long? sinceId, long? maxId, bool extendedTweet)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"tweet_mode", extendedTweet ? "extended" : null}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("favorites/list.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<TwitterStatus> CreateFavoriteAsync(
            this IOAuthCredential credential, long id, bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"tweet_mode", extendedTweet ? "extended" : null}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("favorites/create.json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> DestroyFavoriteAsync(
            this IOAuthCredential credential, long id, bool extendedTweet = true)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id},
                {"tweet_mode", extendedTweet ? "extended" : null}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("favorites/destroy.json"), param);
            return await response.ReadAsStatusAsync();
        }
    }
}