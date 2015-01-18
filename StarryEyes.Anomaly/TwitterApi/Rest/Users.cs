using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Users
    {
        #region users/lookup

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IEnumerable<long> userIds)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (userIds == null) throw new ArgumentNullException("userIds");
            return credential.LookupUserAsync(userIds, CancellationToken.None);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IEnumerable<string> screenNames)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenNames == null) throw new ArgumentNullException("screenNames");
            return credential.LookupUserAsync(screenNames, CancellationToken.None);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IEnumerable<long> userIds,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (userIds == null) throw new ArgumentNullException("userIds");
            return LookupUserCoreAsync(credential, userIds, null, cancellationToken);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IEnumerable<string> screenNames,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenNames == null) throw new ArgumentNullException("screenNames");
            return LookupUserCoreAsync(credential, null, screenNames, cancellationToken);
        }

        private static async Task<IEnumerable<TwitterUser>> LookupUserCoreAsync(
            IOAuthCredential credential, IEnumerable<long> userIds, IEnumerable<string> screenNames,
            CancellationToken cancellationToken)
        {
            var userIdsString = userIds == null
                ? null
                : userIds.Select(s => s.ToString(CultureInfo.InvariantCulture))
                         .JoinString(",");
            var param = new Dictionary<string, object>
            {
                {"user_id", userIdsString},
                {"screen_name", screenNames == null ? null : screenNames.JoinString(",")},
            };
            var resp = await credential.GetAsync("users/lookup.json", param, cancellationToken);
            return await resp.ReadAsUserCollectionAsync();
        }

        #endregion

        #region users/search

        public static Task<IEnumerable<TwitterUser>> SearchUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string query,
            int? page = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            return credential.SearchUserAsync(query, page, count, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterUser>> SearchUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string query,
            int? page, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"q", query},
                {"page", page},
                {"count", count},
            };
            var resp = await credential.GetAsync("users/search.json", param, cancellationToken);
            return await resp.ReadAsUserCollectionAsync();
        }

        #endregion

        #region users/show

        public static Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.ShowUserAsync(userId, CancellationToken.None);
        }

        public static Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return credential.ShowUserAsync(screenName, CancellationToken.None);
        }

        public static Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, long userId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ShowUserCoreAsync(credential, userId, null, cancellationToken);
        }

        public static Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string screenName,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return ShowUserCoreAsync(credential, null, screenName, cancellationToken);
        }

        private static async Task<TwitterUser> ShowUserCoreAsync(
            IOAuthCredential credential, long? userId, string screenName,
            CancellationToken cancellationToken)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            };

            var resp = await credential.GetAsync("users/show.json", param, cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion
    }
}
