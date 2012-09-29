using System;
using System.Collections.Generic;
using StarryEyes.Moon.Api.Parsing;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Moon.Api.Rest
{
    public static class Users
    {
        public static IObservable<TwitterUser> LookupUser(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null, bool include_entities = true)
        {
            if (user_id == null && screen_name == null)
                throw new ArgumentNullException("both of user_id and screen_name are null.");
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/users/lookup.json"))
                .SetParameters(param)
                .GetResponse()
                .ReadUsers();
        }

        // profile image is not implemented... (please teach how to implement it!)

        public static IObservable<TwitterUser> SearchUser(this AuthenticateInfo info,
            string query, int? page = null, int per_page = 20, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"q", query},
                {"page", page},
                {"per_page", per_page},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/users/search.json"))
                .SetParameters(param)
                .GetResponse()
                .ReadUsers();
        }

        public static IObservable<TwitterUser> ShowUser(this AuthenticateInfo info,
            long? user_id = null, string screen_name = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"user_id", user_id},
                {"screen_name", screen_name},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/users/show.json"))
                .SetParameters(param)
                .GetResponse()
                .ReadUser();
        }

        // APIs for contributors are unimplemented. fuck!!!!!!!!!!!!!!!!!!!!!!!!!!

        // suggestions are unimplemented. Is that useful? I'm wondering it!!!!!!!!!!!!
    }
}
