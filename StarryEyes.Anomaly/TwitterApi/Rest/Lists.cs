using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Lists
    {
        #region Lists

        public static Task<IEnumerable<TwitterList>> GetLists(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListsCore(credential, userId, null);
        }

        public static Task<IEnumerable<TwitterList>> GetLists(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListsCore(credential, null, screenName);
        }

        private static async Task<IEnumerable<TwitterList>> GetListsCore(
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

        public static Task<IEnumerable<TwitterStatus>> GetListTimeline(
            this IOAuthCredential credential, long listId,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListTimelineCore(credential, listId, null, null, null,
                                       sinceId, maxId, count, includeRts);
        }

        public static Task<IEnumerable<TwitterStatus>> GetListTimeline(
            this IOAuthCredential credential, string slug, long userId,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            return GetListTimelineCore(credential, null, slug, userId, null,
                                       sinceId, maxId, count, includeRts);
        }

        public static Task<IEnumerable<TwitterStatus>> GetListTimeline(
            this IOAuthCredential credential, string slug, string screenName,
            long? sinceId = null, long? maxId = null, int? count = null, bool? includeRts = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListTimelineCore(credential, null, slug, null, screenName,
                                       sinceId, maxId, count, includeRts);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetListTimelineCore(
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

        public static Task<CursorResult<IEnumerable<TwitterUser>>> GetListMembers(
            this IOAuthCredential credential, long listId,
            int? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return GetListMembersCore(credential, listId, null, null, null,
                                      cursor);
        }

        public static Task<CursorResult<IEnumerable<TwitterUser>>> GetListMembers(
            this IOAuthCredential credential, string slug, long userId,
            int? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            return GetListMembersCore(credential, null, slug, userId, null,
                                      cursor);
        }

        public static Task<CursorResult<IEnumerable<TwitterUser>>> GetListMembers(
            this IOAuthCredential credential, string slug, string screenName,
            int? cursor = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (slug == null) throw new ArgumentNullException("slug");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return GetListMembersCore(credential, null, slug, null, screenName,
                                      cursor);
        }

        private static async Task<CursorResult<IEnumerable<TwitterUser>>> GetListMembersCore(
            IOAuthCredential credential, long? listId, string slug, long? userId, string screenName,
            int? cursor = null)
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
