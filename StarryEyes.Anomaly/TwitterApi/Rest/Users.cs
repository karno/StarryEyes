using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Users
    {
        #region users/lookup

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] IEnumerable<long> userIds)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (userIds == null) throw new ArgumentNullException("userIds");
            return credential.LookupUserAsync(properties, userIds, CancellationToken.None);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] IEnumerable<string> screenNames)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (screenNames == null) throw new ArgumentNullException("screenNames");
            return credential.LookupUserAsync(properties, screenNames, CancellationToken.None);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties,
            [NotNull] IEnumerable<long> userIds, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (userIds == null) throw new ArgumentNullException("userIds");
            return LookupUserCoreAsync(credential, properties, userIds, null, cancellationToken);
        }

        public static Task<IEnumerable<TwitterUser>> LookupUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] IEnumerable<string> screenNames, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (screenNames == null) throw new ArgumentNullException("screenNames");
            return LookupUserCoreAsync(credential, properties, null, screenNames, cancellationToken);
        }

        private static async Task<IEnumerable<TwitterUser>> LookupUserCoreAsync(
            [NotNull] IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            IEnumerable<long> userIds, IEnumerable<string> screenNames, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var userIdsString = userIds == null
                ? null
                : userIds.Select(s => s.ToString(CultureInfo.InvariantCulture))
                         .JoinString(",");
            var param = new Dictionary<string, object>
            {
                {"user_id", userIdsString},
                {"screen_name", screenNames == null ? null : screenNames.JoinString(",")},
            };
            return await credential.GetAsync(properties, "users/lookup.json", param,
                ResultHandlers.ReadAsUserCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region users/search

        public static Task<IEnumerable<TwitterUser>> SearchUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] string query, int? page = null, int? count = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");
            return credential.SearchUserAsync(properties, query, page, count, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterUser>> SearchUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] string query, int? page, int? count, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"q", query},
                {"page", page},
                {"count", count},
            };
            return await credential.GetAsync(properties, "users/search.json", param,
                ResultHandlers.ReadAsUserCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region users/show

        public static Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter parameter)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (parameter == null) throw new ArgumentNullException("parameter");
            return credential.ShowUserAsync(properties, parameter, CancellationToken.None);
        }

        public static async Task<TwitterUser> ShowUserAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter parameter, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (parameter == null) throw new ArgumentNullException("parameter");
            var param = parameter.ToDictionary();
            return await credential.GetAsync(properties, "users/show.json", param,
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
