using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using StarryEyes.Moon.Api.Parsing;
using StarryEyes.Moon.Api.Parsing.JsonFormats;
using StarryEyes.Moon.Authorize;

namespace StarryEyes.Moon.Api.Rest
{
    public static class Trends
    {
        public static IObservable<TrendInfoJson> GetTrends(this AuthenticateInfo info, int woeid = 1, bool? exclude_hashtags = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"exclude", exclude_hashtags}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/trends/" + woeid + ".json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<TrendInfoJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _);
        }

        public static IObservable<TrendAvailableInfoJson> GetAvailableTrends(this AuthenticateInfo info,
            long? geo_lat = null, long? geo_long = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"lat", geo_lat},
                {"long", geo_long},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetEndpoint(ApiEndpoint.EndpointApiV1.JoinUrl("/trends/available.json"))
                .SetParameters(param)
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJsonArray<TrendAvailableInfoJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _);
        }

        public static IObservable<TrendAvailableInfoJson> GetAvailableTrends()
        {
            var req = HttpWebRequest.Create(ApiEndpoint.EndpointApiV1.JoinUrl("/trends/available.json"));
            return Observable.FromAsyncPattern<WebResponse>(req.BeginGetResponse, req.EndGetResponse)()
                .ReadString()
                .DeserializeJsonArray<TrendAvailableInfoJson>()
                .Where(_ => _ != null)
                .SelectMany(_ => _);
        }
    }
}
