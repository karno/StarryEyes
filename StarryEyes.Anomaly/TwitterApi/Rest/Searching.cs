using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Searching
    {
        #region search/tweets

        public static Task<IEnumerable<TwitterStatus>> SearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] SearchParameter query)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");

            return credential.SearchAsync(query, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> SearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] SearchParameter query,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");

            var response = await credential.GetAsync("search/tweets.json",
                query.ToDictionary(), cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region saved_searches/list

        public static Task<IEnumerable<TwitterSavedSearch>> GetSavedSearchesAsync(
            [NotNull] this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetSavedSearchesAsync(CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterSavedSearch>> GetSavedSearchesAsync(
            [NotNull] this IOAuthCredential credential, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var respStr = await credential.GetStringAsync("saved_searches/list.json",
                new Dictionary<string, object>(), cancellationToken);
            return await Task.Run(() =>
            {
                var parsed = DynamicJson.Parse(respStr);
                return ((dynamic[])parsed).Select(
                    item => new TwitterSavedSearch(item));
            }, cancellationToken);
        }

        #endregion

        #region saved_searches/create

        public static Task<TwitterSavedSearch> SaveSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string query)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            return credential.SaveSearchAsync(query, CancellationToken.None);

        }

        public static async Task<TwitterSavedSearch> SaveSearchAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] string query,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (query == null) throw new ArgumentNullException("query");
            var param = new Dictionary<string, object>
            {
                {"query", query}
            };
            var response = await credential.PostAsync("saved_searches/create.json",
                param, cancellationToken);
            var respStr = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterSavedSearch(DynamicJson.Parse(respStr)),
                cancellationToken);
        }

        #endregion

        #region saved_searches/destroy

        public static Task<TwitterSavedSearch> DestroySavedSearchAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.DestroySavedSearchAsync(id, CancellationToken.None);
        }

        public static async Task<TwitterSavedSearch> DestroySavedSearchAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var response = await credential.PostAsync("saved_searches/destroy/" + id + ".json",
                new Dictionary<string, object>(), cancellationToken);
            var respStr = await response.ReadAsStringAsync();
            return await Task.Run(() => new TwitterSavedSearch(DynamicJson.Parse(respStr)),
                cancellationToken);
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
