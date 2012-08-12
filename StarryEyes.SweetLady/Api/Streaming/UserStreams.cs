using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Newtonsoft.Json;
using StarryEyes.SweetLady.Api.Parsing.JsonFormats;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.SweetLady.Api.Streaming
{
    public static class UserStreams
    {
        private static readonly string EndpointUserStreams = "https://userstream.twitter.com/2/user.json";

        public static IObservable<TwitterStreamingElement> ConnectToUserStreams(this AuthenticateInfo info,
            IEnumerable<string> trackKeywords = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"track", trackKeywords != null ? trackKeywords.JoinString(",") : null},
            }.Parametalize();
            return info.GetOAuthClient(useGzip: false)
                .SetEndpoint(EndpointUserStreams)
                .GetResponse()
                .SelectMany(res => res.DownloadStringLineAsync())
                .Where(s => !String.IsNullOrEmpty(s))
                .ObserveOn(Scheduler.ThreadPool)
                .ParseStreamingElement();
        }

        private static Type[] candidates = new Type[] 
        { 
            typeof(TweetJson),
            typeof(DirectMessageJson), 
            typeof(StreamingDeleteJson), 
            typeof(StreamingTrackJson),
        };

        private static T DeserializeJson<T>(this string s)
        {
            return new StringReader(s)
                .Using(sr => new JsonTextReader(sr)
                    .Using(jtr => new JsonSerializer()
                        .Deserialize<T>(jtr)));
        }

        private static TwitterStreamingElement CheckSpawn(this ITwitterStreamingElementSpawnable s)
        {
            if (s == null)
                return null;
            else
                return s.Spawn();
        }

        private static IObservable<TwitterStreamingElement> ParseStreamingElement(this IObservable<string> observable)
        {
            return observable
                .Select<string, TwitterStreamingElement>(s =>
                {
                    EventType eventType;
                    var desz = s.DeserializeJson<StreamingEventJson>();
                    eventType = (desz == null ? null : desz.event_kind).ToEventType();
                    switch (eventType)
                    {
                        case EventType.Undefined:
                            var tweet = s.DeserializeJson<TweetJson>();
                            if (tweet != null && tweet.id_str != null)
                                return new TwitterStreamingElement()
                                {
                                    Status = tweet.Spawn(),
                                };
                            var dmsg = s.DeserializeJson<DirectMessageJson>();
                            if (dmsg != null && dmsg.id_str != null)
                                return new TwitterStreamingElement()
                                {
                                    Status = dmsg.Spawn(),
                                };
                            var deleted = s.DeserializeJson<StreamingDeleteJson>();
                            if (deleted != null && deleted.status != null)
                                return deleted.Spawn();
                            var track = s.DeserializeJson<StreamingTrackJson>();
                            if (track != null && track.track != 0)
                                return new TwitterStreamingElement()
                                {
                                    TrackLimit = track.track,
                                };
                            return s.DeserializeJson<StreamingAdditionalJson>().CheckSpawn();
                        case DataModel.EventType.Follow:
                        case DataModel.EventType.Unfollow:
                        case DataModel.EventType.Blocked:
                        case DataModel.EventType.Unblocked:
                            return s.DeserializeJson<StreamingUserEventJson>().CheckSpawn();
                        case DataModel.EventType.Favorite:
                        case DataModel.EventType.Unfavorite:
                            return s.DeserializeJson<StreamingTweetEventJson>().CheckSpawn();
                        case DataModel.EventType.ListCreated:
                        case DataModel.EventType.ListUpdated:
                        case DataModel.EventType.ListDeleted:
                        case DataModel.EventType.ListSubscribed:
                        case DataModel.EventType.ListUnsubscribed:
                        case DataModel.EventType.ListMemberAdded:
                        case DataModel.EventType.ListMemberRemoved:
                            return s.DeserializeJson<StreamingEventJson>().CheckSpawn();
                        default:
                            System.Diagnostics.Debug.WriteLine("undecodable:" + s);
                            return null;
                    }
                })
                .Where(s => s != null);
        }
    }
}
