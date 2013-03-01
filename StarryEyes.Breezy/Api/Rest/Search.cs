using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Search
    {

        public static IObservable<TwitterStatus> SearchTweets(this AuthenticateInfo info,
            string query, string geocode = null, string lang = null, string locale = null,
            SearchResultType result_type = SearchResultType.mixed, int count = 100, string until = null,
            long? since_id = null, long? max_id = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"q", query},
                {"geocode", geocode},
                {"lang", lang},
                {"locale", locale},
                {"result_type", result_type.ToString()},
                {"count", count},
                {"until", until},
                {"since_id", since_id},
                {"max_id", max_id},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("search/tweets.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<SearchJson>()
                .Where(_ => _ != null)
                .SelectMany(s => s.statuses)
                .Where(_ => _ != null)
                .Select(s => s.Spawn());
        }

        public static IObservable<SavedSearchJson> GetSavedSearchs(this AuthenticateInfo info)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/saved_searches/list.json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<SavedSearchJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _);
        }

        public static IObservable<SavedSearchJson> CreateSavedSearch(this AuthenticateInfo info, string query)
        {
            var param = new Dictionary<string, object>()
            {
                {"query", query},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/saved_searches/create.json"))
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadString()
                .DeserializeJson<SavedSearchJson>();
        }

        public static IObservable<SavedSearchJson> DestroySavedSearch(this AuthenticateInfo info, long id)
        {
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/saved_searches/destroy/" + id + ".json"))
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .GetResponse()
                .ReadString()
                .DeserializeJson<SavedSearchJson>();
        }
    }
}
