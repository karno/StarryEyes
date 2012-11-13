using System;
using System.Collections.Generic;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Favorites
    {
        public static IObservable<TwitterStatus> GetFavorites(this AuthenticateInfo info,
            long? id = null, int? count = null, long? since_id = null, long? max_id = null,
            int? page = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"id", id},
                {"count", count},
                {"since_id", since_id},
                {"max_id", max_id},
                {"page", page},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/favorites/list.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTimeline();
        }

        public static IObservable<TwitterStatus> CreateFavorite(this AuthenticateInfo info,
            long id, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"include_entities", include_entities},
                {"id", id},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/favorites/create.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadTweet();
        }

        public static IObservable<TwitterStatus> DestroyFavorite(this AuthenticateInfo info,
            long id, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"include_entities", include_entities},
                {"id", id},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/favorites/destroy.json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetParameters(param)
                .GetResponse()
                .ReadTweet();
        }
    }
}
