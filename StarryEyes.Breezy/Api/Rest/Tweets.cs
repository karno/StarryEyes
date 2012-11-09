using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Net;

namespace StarryEyes.Breezy.Api.Rest
{
    public static class Tweets
    {
        public static IObservable<long> GetMyRetweetId(this AuthenticateInfo info,
            long id)
        {
            var param = new Dictionary<string, object>()
            {
                {"include_entities", true},
                {"include_my_retweet", true},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/show/" + id + ".json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadString()
                .DeserializeJson<TweetJson>()
                .Select(s => s.current_user_retweet != null ? long.Parse(s.current_user_retweet.id_str) : 0);
        }

        public static IObservable<TwitterUser> GetRetweets(this AuthenticateInfo info,
            long id, int count = 20, bool? trim_user = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"count", count},
                {"trim_user", trim_user},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/retweets/" + id + "json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadUsers();
        }

        public static IObservable<TwitterStatus> ShowTweet(this AuthenticateInfo info,
            long id, bool? trim_user = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"trim_user", trim_user},
                {"include_entities", include_entities},
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/show/" + id + ".json"))
                .GetResponse()
                .UpdateRateLimitInfo(info)
                .ReadTweet();
        }

        public static IObservable<TwitterStatus> Retweet(this AuthenticateInfo info,
            long id, bool? trim_user = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"trim_user", trim_user},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/retweet/" + id + ".json"))
                .GetResponse()
                .ReadTweet();
        }

        public static IObservable<TwitterStatus> Update(this AuthenticateInfo info,
            string status, long? in_reply_to_status_id = null, double? geo_lat = null, double? geo_long = null,
            string place_id = null, bool? display_coordinates = null, bool? trim_user = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"status", status},
                {"in_reply_to_status_id", in_reply_to_status_id},
                {"lat", geo_lat},
                {"long", geo_long},
                {"place_id", place_id},
                {"display_coordinates", display_coordinates},
                {"trim_user", trim_user},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/update.json"))
                .GetResponse()
                .ReadTweet();
        }

        public static IObservable<TwitterStatus> UpdateWithMedia(this AuthenticateInfo info,
            string status, byte[] image, string imageFileName, bool possibly_sensitive = false, long? in_reply_to_status_id = null, double? geo_lat = null, double? geo_long = null,
            string place_id = null, bool? display_coordinates = null, bool sendInBackground = false)
        {
            return Observable.Start(() => new Unit())
                .SelectMany(ms =>
                {
                    var param = new Dictionary<string, object>()
                    {
                        {"status", status},
                        {"in_reply_to_status_id", in_reply_to_status_id},
                        {"possibly_sensitive", possibly_sensitive},
                        {"lat", geo_lat},
                        {"long", geo_long},
                        {"place_id", place_id},
                        {"display_coordinates", display_coordinates},
                    }.Parametalize();
                    return new MultipartableOAuthClient(ApiEndpoint.DefaultConsumerKey, ApiEndpoint.DefaultConsumerSecret, info.AccessToken)
                        {
                            Url = ApiEndpoint.EndpointUpload.JoinUrl("/statuses/update_with_media.json"),
                        }
                        .GetResponse(param.Select(p => new UploadContent(p.Key, p.Value))
                            .Append(new UploadContent("media[]", imageFileName,
                                new Dictionary<string, string>(){
                                    {"Content-Type", "application/octet/stream"},
                                    {"Content-Transfer-Encoding", "binary"}
                                },
                                (s, sw) => s.Write(image, 0, image.Length))))
                        .ReadTweet();
                });
        }
    
        public static IObservable<TwitterStatus> Destroy(this AuthenticateInfo info,
            long id, bool? trim_user = null, bool include_entities = true)
        {
            var param = new Dictionary<string, object>()
            {
                {"trim_user", trim_user},
                {"include_entities", include_entities}
            }.Parametalize();
            return info.GetOAuthClient()
                .SetParameters(param)
                .SetMethodType(Codeplex.OAuth.MethodType.Post)
                .SetEndpoint(ApiEndpoint.EndpointApiV1a.JoinUrl("/statuses/destroy/" + id + ".json"))
                .GetResponse()
                .ReadTweet();
        }
    }

    public enum SearchResultType
    {
        mixed,
        recent,
        popular
    }
}
