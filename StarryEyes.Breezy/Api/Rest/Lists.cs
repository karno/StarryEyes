using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Codeplex.OAuth;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Lists
    {
        public static IObservable<TwitterList> GetListsAll(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/list.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadLists();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterList> GetList(this AuthenticateInfo info,
            long? list_id = null, string slug = null,
            long? owner_id = null, string owner_screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("lists/show.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadList();
        }

        public static IObservable<TwitterList> CreateList(this AuthenticateInfo info,
            string name, bool isPrivate = false, string description = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"name", name},
                {"mode", isPrivate ? "private" : "public"},
                {"description", description},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/members/create.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadList();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterList> UpdateList(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            string name = null, bool? isPrivate = null, string description = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
                {"name", name},
                {"mode", isPrivate.HasValue ? (isPrivate.Value ? "private" : "public") : null},
                {"description", description}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/update.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadList();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterList> DestroyList(this AuthenticateInfo info,
            long list_id, string slug, long? owner_id = null, string owner_screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/destroy.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadList();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterStatus> GetListStatuses(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            long? since_id = null, long? max_id = null, int? per_page = null,
            bool? include_entities = true, bool? include_rts = false)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_screen_name", owner_screen_name},
                {"owner_id", owner_id},
                {"since_id", since_id},
                {"max_id", max_id},
                {"per_page", per_page},
                {"include_entities", include_entities},
                {"include_rts", include_rts},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/statuses.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        private static IObservable<TwitterUser> GetMembersAllSink(this AuthenticateInfo info,
            string endpoint, Dictionary<string, object> param, long cursor = -1)
        {
            if (cursor == 0)
                return Observable.Empty<TwitterUser>();
            var p = param.Parametalize();
            p.Add(new Parameter("cursor", cursor));
            long next_cursor = 0;
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl(endpoint))
                .SetParameters(p)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<UserCollectionJson>()
                .Do(ucj => next_cursor = long.Parse(ucj.next_cursor_str))
                .SelectMany(ucj => ucj.users.Select(u => u.Spawn()))
                .Concat(info.GetMembersAllSink(endpoint, param, next_cursor))
                .Catch(Observable.Empty<TwitterUser>());
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> GetListMembersAll(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            bool include_entities = true, bool? skip_status = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_screen_name", owner_screen_name},
                {"owner_id", owner_id},
                {"include_entities", include_entities},
                {"skip_status", skip_status},
            };
            return info.GetMembersAllSink("lists/members.json", param);
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> AddListMember(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            long? user_id = null, string screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/members/create.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUser();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> AddListMembers(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            IEnumerable<long> user_id = null, IEnumerable<string> screen_name = null)
        {
            string userIdsArray = null;
            if (user_id != null)
                userIdsArray = user_id.Select(l => l.ToString()).JoinString(",");
            string screenNamesArray = null;
            if (screen_name != null)
                screenNamesArray = screen_name.JoinString(",");
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"user_id", userIdsArray},
                {"screen_name", screenNamesArray},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/members/create_all.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUsers();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> RemoveListMember(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            long? user_id = null, string screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/members/destroy.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUser();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> RemoveListMembers(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            IEnumerable<long> user_id = null, IEnumerable<string> screen_name = null)
        {
            string userIdsArray = null;
            if (user_id != null)
                userIdsArray = user_id.Select(l => l.ToString()).JoinString(",");
            string screenNamesArray = null;
            if (screen_name != null)
                screenNamesArray = screen_name.JoinString(",");
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"user_id", userIdsArray},
                {"screen_name", screenNamesArray},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/members/destroy_all.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUsers();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterUser> GetListSubscribersAll(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null,
            bool? include_entities = true, bool? skip_status = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_screen_name", owner_screen_name},
                {"owner_id", owner_id},
                {"include_entities", include_entities},
                {"skip_status", skip_status},
            };
            return info.GetMembersAllSink("lists/subscribers.json", param);
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterList> SubscribeList(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/subscribers/create.json"))
                .SetParameters(param)
                .SetMethodType(MethodType.Post)
                .GetResponse()
                .ReadList();
        }

        /// <summary>
        /// Either a list_id or a slug is required.<para />
        /// If providing a list_slug, an owner_screen_name or an owner_id is also required.
        /// </summary>
        public static IObservable<TwitterList> UnsubscribeList(this AuthenticateInfo info,
            long? list_id = null, string slug = null, long? owner_id = null, string owner_screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"list_id", list_id},
                {"slug", slug},
                {"owner_id", owner_id},
                {"owner_screen_name", owner_screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/lists/subscribers/destroy.json"))
                .SetParameters(param)
                .SetMethodType(MethodType.Post)
                .GetResponse()
                .ReadList();
        }
    }
}