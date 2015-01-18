using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Favorites
    {
        #region favorites/list

        public static Task<IEnumerable<TwitterStatus>> GetFavoritesAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter targetUser,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            return credential.GetFavoritesAsync(targetUser, count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetFavoritesAsync(
            [NotNull] this IOAuthCredential credential, [CanBeNull] UserParameter targetUser,
            int? count, long? sinceId, long? maxId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            }.ApplyParameter(targetUser);
            var response = await credential.GetAsync("favorites/list.json", param, cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region favorites/create

        public static Task<TwitterStatus> CreateFavoriteAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.CreateFavoriteAsync(id, CancellationToken.None);
        }

        public static async Task<TwitterStatus> CreateFavoriteAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            var response = await credential.PostAsync("favorites/create.json", param, cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion

        #region favorites/destroy

        public static Task<TwitterStatus> DestroyFavoriteAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.DestroyFavoriteAsync(id, CancellationToken.None);
        }

        public static async Task<TwitterStatus> DestroyFavoriteAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            var response = await credential.PostAsync("favorites/destroy.json", param, cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion
    }
}
