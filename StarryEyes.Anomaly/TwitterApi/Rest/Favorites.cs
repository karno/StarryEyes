using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Favorites
    {
        #region favorites/list

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetFavoritesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter targetUser, int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetFavoritesAsync(properties, targetUser, count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetFavoritesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] UserParameter targetUser, int? count, long? sinceId, long? maxId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            }.ApplyParameter(targetUser);
            return await credential.GetAsync(properties, "favorites/list.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region favorites/create

        public static Task<IApiResult<TwitterStatus>> CreateFavoriteAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.CreateFavoriteAsync(properties, id, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterStatus>> CreateFavoriteAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            return await credential.PostAsync(properties, "favorites/create.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region favorites/destroy

        public static Task<IApiResult<TwitterStatus>> DestroyFavoriteAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.DestroyFavoriteAsync(properties, id, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterStatus>> DestroyFavoriteAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            return await credential.PostAsync(properties, "favorites/destroy.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
