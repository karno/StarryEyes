using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Lists
    {
        #region Show list data

        public static Task<TwitterList> ShowListAsync(
            this IOAuthCredential credential, long listId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ShowListCoreAsync(credential, listId, null, null, null);
        }

        public static Task<TwitterList> ShowListAsync(
            this IOAuthCredential credential, long userId, string slug)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            return ShowListCoreAsync(credential, null, userId, null, slug);
        }

        public static Task<TwitterList> ShowListAsync(
            this IOAuthCredential credential, string screenName, string slug)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            if (slug == null) throw new ArgumentNullException("slug");
            return ShowListCoreAsync(credential, null, null, screenName, slug);
        }

        private static async Task<TwitterList> ShowListCoreAsync(
            IOAuthCredential credential, long? listId, long? userId, string screenName, string slug)
        {
            var param = new Dictionary<string, object>
            {
                {"list_id", listId},
                {"owner_id", userId},
                {"owner_screen_name", screenName},
                {"slug", slug},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("lists/show.json", param));
            return await response.ReadAsListAsync();
        }

        #endregion

        #region Lists

        public static Task<IEnumerable<TwitterList>> GetListsAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListsCoreAsync(credential, userId, null);
        }

        public static Task<IEnumerable<TwitterList>> GetListsAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListsCoreAsync(credential, null, screenName);
        }

        private static async Task<IEnumerable<TwitterList>> GetListsCoreAsync(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("lists/list.json", param));
            return await response.ReadAsListCollectionAsync();
        }

        #endregion

        #region Statuses

        public static Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            this IOAuthCredential credential, long listId,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListTimelineCoreAsync(
                credential, listId, null, null, null, sinceId, maxId, count, includeRts);
        }

        public static Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            this IOAuthCredential credential, string slug, long userId,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            return GetListTimelineCoreAsync(
                credential, null, slug, userId, null, sinceId, maxId, count, includeRts);
        }

        public static Task<IEnumerable<TwitterStatus>> GetListTimelineAsync(
            this IOAuthCredential credential, string slug, string screenName,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListTimelineCoreAsync(
                credential, null, slug, null, screenName, sinceId, maxId, count, includeRts);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetListTimelineCoreAsync(
            IOAuthCredential credential, long? listId, string slug, long? userId, string screenName,
            long? sinceId, long? maxId, int? count, bool? includeRts)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", listId},
                {"slug", slug},
                {"owner_id", userId},
                {"owner_screen_name", screenName},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"count", count},
                {"include_rts", includeRts},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("lists/statuses.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region Memberships

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            this IOAuthCredential credential, long listId,
            long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListMembersCoreAsync(
                credential, listId, null, null, null, cursor);
        }

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            this IOAuthCredential credential, string slug, long userId,
            long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            return GetListMembersCoreAsync(
                credential, null, slug, userId, null, cursor);
        }

        public static Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersAsync(
            this IOAuthCredential credential, string slug, string screenName,
            long? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListMembersCoreAsync(
                credential, null, slug, null, screenName, cursor);
        }

        private static async Task<ICursorResult<IEnumerable<TwitterUser>>> GetListMembersCoreAsync(
            IOAuthCredential credential, long? listId, string slug, long? userId, string screenName,
            long? cursor = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", listId},
                {"slug", slug},
                {"owner_id", userId},
                {"owner_screen_name", screenName},
                {"cursor", cursor},
                {"skip_status", true},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("lists/members.json", param));
            return await response.ReadAsCursoredUsersAsync();
        }

        #endregion
    }
}
