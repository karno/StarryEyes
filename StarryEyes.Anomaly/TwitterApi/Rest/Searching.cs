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
    public static class Searching
    {
        #region search/tweets

        public static Task<IEnumerable<TwitterStatus>> SearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] SearchParameter query)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");

            return credential.SearchAsync(properties, query, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> SearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] SearchParameter query, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");

            return await credential.GetAsync(properties, "search/tweets.json", query.ToDictionary(),
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region saved_searches/list

        public static Task<IEnumerable<TwitterSavedSearch>> GetSavedSearchesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetSavedSearchesAsync(properties, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterSavedSearch>> GetSavedSearchesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.GetAsync(properties, "saved_searches/list.json", new Dictionary<string, object>(),
                ResultHandlers.ReadAsSavedSearchCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region saved_searches/create

        public static Task<TwitterSavedSearch> SaveSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, [NotNull] string query)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");
            return credential.SaveSearchAsync(properties, query, CancellationToken.None);

        }

        public static async Task<TwitterSavedSearch> SaveSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] string query, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"query", query}
            };
            return await credential.PostAsync(properties, "saved_searches/create.json", param,
                ResultHandlers.ReadAsSavedSearchAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region saved_searches/destroy

        public static Task<TwitterSavedSearch> DestroySavedSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.DestroySavedSearchAsync(properties, id, CancellationToken.None);
        }

        public static async Task<TwitterSavedSearch> DestroySavedSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.PostAsync(properties, "saved_searches/destroy/" + id + ".json",
                new Dictionary<string, object>(), ResultHandlers.ReadAsSavedSearchAsync, cancellationToken)
                                   .ConfigureAwait(false);
        }

        #endregion
    }

    public enum SearchResultType
    {
        Mixed,
        Recent,
        Popular,
    }
}
