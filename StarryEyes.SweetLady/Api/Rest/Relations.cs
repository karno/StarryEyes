using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Api.Parsing;
using StarryEyes.SweetLady.Api.Parsing.JsonFormats;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.SweetLady.Api.Rest
{
    public static class Relations
    {
        #region Friendships

        public static IObservable<long> GetFollowerIdsAll(this AuthenticateInfo info,
            long? userId = null, string screenName = null)
        {
            string endpoint = ApiEndpoint.EndpointApiV1.JoinUrl("/followers/ids.json");
            return info.GetIdsAllSink(endpoint, userId, screenName);
        }

        public static IObservable<long> GetFriendsIdsAll(this AuthenticateInfo info,
            long? userId = null, string screenName = null)
        {
            string endpoint = ApiEndpoint.EndpointApiV1.JoinUrl("/friends/ids.json");
            return info.GetIdsAllSink(endpoint, userId, screenName);
        }

        public static IObservable<long> GetIncomingFriendshipsAll(this AuthenticateInfo info)
        {
            string endpoint = ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/incoming.json");
            return info.GetIdsAllSink(endpoint);
        }

        public static IObservable<long> GetOutgoingFriendshipsAll(this AuthenticateInfo info)
        {
            string endpoint = ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/outgoing.json");
            return info.GetIdsAllSink(endpoint);
        }

        public static IObservable<FriendshipJson> GetFriendship(this AuthenticateInfo info,
            long? source_id = null, string source_screen_name = null,
            long? target_id = null, string target_screen_name = null)
        {
            if (source_id == null && source_screen_name == null)
                throw new NullReferenceException("both of source_id and source_screen_name are null.");
            if (target_id == null && target_screen_name == null)
                throw new NullReferenceException("both of source_id and source_screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"source_id", source_id},
                {"source_screen_name", source_screen_name},
                {"target_id", target_id},
                {"target_screen_name", target_screen_name}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/show.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<FriendshipJson>();
        }

        public static IObservable<TwitterUser> CreateFriendship(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null, bool enableNotification = false)
        {
            if (user_id == null && screen_name == null)
                throw new NullReferenceException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"follow", enableNotification},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/create.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUser();
        }

        public static IObservable<TwitterUser> DestroyFriendship(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null)
        {
            if (user_id == null && screen_name == null)
                throw new NullReferenceException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/destroy.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadUser();
        }

        public static IObservable<LookupInfoJson> LookupFriendship(this AuthenticateInfo info,
            IEnumerable<long> userIds, IEnumerable<string> screenNames)
        {
            if (userIds == null && screenNames == null)
                throw new NullReferenceException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", userIds.Select(l => l.ToString()).JoinString(",")},
                {"screen_name", screenNames.JoinString(",")}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/lookup.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<LookupInfoJson>()
                .SelectMany(_ => _);
        }

        public static IObservable<FriendshipJson> UpdateFriendship(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null,
            bool? device = null, bool? retweets = null)
        {
            throw new NotImplementedException();
        }

        public static IObservable<long> GetNoRetweetIds(this AuthenticateInfo info)
        {
            var param = new Dictionary<string, object>()
            {
                {"stringify_ids", true}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/friendships/no_retweet_ids.json"))
                .SetParameters(param)
                .GetResponse()
                .ReadIdsArray();
        }

        private static IObservable<long> GetIdsAllSink(this AuthenticateInfo info,
            string endpoint, long? user_id = null, string screen_name = null, long cursor = -1)
        {
            if (cursor == 0)
                return Observable.Empty<long>();
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"cursor", cursor},
                {"stringify_ids", true}
            }.Parametalize();
            long next_cursor = 0;
            return info.GetOAuthClient()
                .SetEndpoint(endpoint)
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<IdsJson>()
                .Do(idj => next_cursor = long.Parse(idj.next_cursor_str))
                .SelectMany(idj => idj.ids.Select(s => long.Parse(s)))
                .Concat(info.GetIdsAllSink(endpoint, user_id, screen_name, next_cursor))
                .Catch(Observable.Empty<long>());
        }

        #endregion

        #region Block and R4S

        public static IObservable<long> GetBlockingsIdsAll(this AuthenticateInfo info)
        {
            string endpoint = ApiEndpoint.EndpointApiV1.JoinUrl("/blocks/blocking/ids.json");
            return info.GetIdsAllSink(endpoint);
        }

        public static IObservable<TwitterUser> CreateBlock(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null, bool include_entities = true, bool? skip_status = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"include_entities", include_entities},
                {"skip_status", skip_status},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/blocks/create.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadUser();
        }

        public static IObservable<TwitterUser> DestroyBlock(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null, bool include_entities = true, bool? skip_status = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"include_entities", include_entities},
                {"skip_status", skip_status},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/blocks/destroy.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadUser();
        }

        public static IObservable<TwitterUser> ReportSpam(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/report_spam.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadUser();
        }


        #endregion
    }
}