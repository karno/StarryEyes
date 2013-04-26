using System;
using System.Collections.Generic;
using System.Linq;
using Codeplex.OAuth;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Users
    {
        public static IObservable<TwitterUser> LookupUser(this AuthenticateInfo info,
            IEnumerable<long> userId = null, IEnumerable<string> screenName = null, bool includeEntities = true)
        {
            if (userId == null && screenName == null)
                throw new ArgumentNullException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>
            {
                {"user_id", userId == null ? null : userId.Select(s => s.ToString()).JoinString(",")},
                {"screen_name", screenName == null ? null : screenName.JoinString(",")},
                {"include_entities", includeEntities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/users/lookup.json"))
                .SetMethodType(MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadUsers();
        }

        // profile image is not implemented... (please teach how to implement it!)

        public static IObservable<TwitterUser> SearchUser(this AuthenticateInfo info,
            string query, int? page = null, int count = 20, bool includeEntities = true)
        {
            var param = new Dictionary<string, object>
            {
                {"q", query},
                {"page", page},
                {"count", count},
                {"include_entities", includeEntities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/users/search.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadUsers();
        }

        public static IObservable<TwitterUser> ShowUser(this AuthenticateInfo info,
            long? userId = null, string screenName = null, bool includeEntities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", userId},
                {"screen_name", screenName},
                {"include_entities", includeEntities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/users/show.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadUser();
        }

        // APIs for contributors are unimplemented. fuck the contributor API!!!!!!!!!!!!!!!!!!!!!!!!!!

        // suggestions are unimplemented. Is that useful? I'm wondering it!!!!!!!!!!!!
    }
}
