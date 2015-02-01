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
    public static class Lists
    {
        #region lists/show

        public static Task<TwitterList> ShowListAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return credential.ShowListAsync(properties, targetList, CancellationToken.None);
        }

        public static async Task<TwitterList> ShowListAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return await credential.GetAsync(properties, "lists/show.json", targetList.ToDictionary(),
                ResultHandlers.ReadAsListAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region lists/list

        public static Task<IEnumerable<TwitterList>> GetListsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return credential.GetListsAsync(properties, targetList, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterList>> GetListsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");

            return await credential.GetAsync(properties, "lists/list.json", targetList.ToDictionary(),
                ResultHandlers.ReadAsListCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region lists/statuses

        public static Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter listTarget, long? sinceId = null, long? maxId = null,
            int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetListTimelineAsync(properties, listTarget, sinceId, maxId, count, includeRts,
                CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter listTarget, long? sinceId, long? maxId, int? count, bool? includeRts,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>()
            {
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"include_rts", includeRts},
            }.ApplyParameter(listTarget);
            return await credential.GetAsync(properties, "lists/statuses.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Memberships

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList, long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return credential.GetListMembersAsync(properties, targetList, cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] ListParameter targetList, long? cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetList == null) throw new ArgumentNullException("targetList");
            var param = new Dictionary<string, object>()
            {
                {"cursor", cursor},
                {"skip_status", true},
            }.ApplyParameter(targetList);
            return await credential.GetAsync(properties, "lists/members.json", param,
                ResultHandlers.ReadAsCursoredUsersAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
