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
    public static class Lists
    {
        #region lists/show

        public static Task<TwitterList> ShowListAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return credential.ShowListAsync(targetList, CancellationToken.None);
        }

        public static async Task<TwitterList> ShowListAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var response = await credential.GetAsync("lists/show.json", targetList.ToDictionary(), cancellationToken);
            return await response.ReadAsListAsync();
        }

        #endregion

        #region lists/list

        public static Task<IEnumerable<TwitterList>> GetListsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return credential.GetListsAsync(targetList, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterList>> GetListsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");

            var response = await credential.GetAsync("lists/list.json", targetList.ToDictionary(), cancellationToken);
            return await response.ReadAsListCollectionAsync();
        }

        #endregion

        #region lists/statuses

        public static Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter listTarget,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListTimelineAsync(credential, listTarget, sinceId, maxId, count, includeRts,
                CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter listTarget,
            long? sinceId, long? maxId, int? count, bool? includeRts,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>()
            {
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"include_rts", includeRts},
            }.ApplyParameter(listTarget);
            var response = await credential.GetAsync("lists/statuses.json", param, cancellationToken);
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region Memberships

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList,
            long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");
            return GetListMembersAsync(credential, targetList, cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] ListParameter targetList,
            long? cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetList == null) throw new ArgumentNullException("targetList");
            var param = new Dictionary<string, object>()
            {
                {"cursor", cursor},
                {"skip_status", true},
            }.ApplyParameter(targetList);
            var response = await credential.GetAsync("lists/members.json", param, cancellationToken);
            return await response.ReadAsCursoredUsersAsync();
        }

        #endregion
    }
}
