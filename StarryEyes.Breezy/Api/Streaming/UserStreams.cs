using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Codeplex.OAuth;
using Newtonsoft.Json;
using StarryEyes.Breezy.Api.Parsing.JsonFormats;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Breezy.Api.Streaming
{
    public static class UserStreams
    {
        private const string EndpointUserStreams = "https://userstream.twitter.com/2/user.json";

        public static IObservable<TwitterStreamingElement> ConnectToUserStreams(this AuthenticateInfo info,
                                                                                IEnumerable<string> trackKeywords = null)
        {
            var paradic = new Dictionary<string, object>();
            string tracks = trackKeywords != null ? trackKeywords.Distinct().JoinString(",") : null;
            if (!String.IsNullOrWhiteSpace(tracks))
            {
                paradic.Add("track", tracks);
            }
            ParameterCollection param = paradic.Parametalize();
            return info.GetOAuthClient(useGzip: false)
                       .SetEndpoint(EndpointUserStreams)
                       .SetParameters(param)
                       .GetResponse()
                       .SelectMany(res => res.DownloadStringLineAsync())
                       .Where(s => !String.IsNullOrEmpty(s))
                       .ObserveOn(TaskPoolScheduler.Default)
                       .ParseStreamingElement();
        }

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
            return s.Spawn();
        }

        private static IObservable<TwitterStreamingElement> ParseStreamingElement(this IObservable<string> observable)
        {
            return observable
                .Select(s =>
                {
                    var desz = s.DeserializeJson<StreamingEventJson>();
                    EventType eventType = (desz == null ? null : desz.event_kind).ToEventType();
                    switch (eventType)
                    {
                        case EventType.Empty:
                            var tweet = s.DeserializeJson<TweetJson>();
                            if (tweet != null && tweet.id_str != null)
                                return new TwitterStreamingElement
                                {
                                    Status = tweet.Spawn(),
                                };
                            var dmsg = s.DeserializeJson<DirectMessageJson>();
                            if (dmsg != null && dmsg.id_str != null)
                                return new TwitterStreamingElement
                                {
                                    Status = dmsg.Spawn(),
                                };
                            var deleted = s.DeserializeJson<StreamingDeleteJson>();
                            if (deleted != null && deleted.status != null)
                                return deleted.Spawn();
                            var track = s.DeserializeJson<StreamingTrackJson>();
                            if (track != null && track.track != 0)
                                return new TwitterStreamingElement
                                {
                                    EventType = EventType.LimitationInfo,
                                    TrackLimit = track.track,
                                };
                            return s.DeserializeJson<StreamingAdditionalJson>().CheckSpawn();
                        case EventType.Follow:
                        case EventType.Unfollow:
                        case EventType.Blocked:
                        case EventType.Unblocked:
                            return s.DeserializeJson<StreamingUserEventJson>().CheckSpawn();
                        case EventType.Favorite:
                        case EventType.Unfavorite:
                            return s.DeserializeJson<StreamingTweetEventJson>().CheckSpawn();
                        case EventType.ListCreated:
                        case EventType.ListUpdated:
                        case EventType.ListDeleted:
                        case EventType.ListSubscribed:
                        case EventType.ListUnsubscribed:
                        case EventType.ListMemberAdded:
                        case EventType.ListMemberRemoved:
                            return s.DeserializeJson<StreamingEventJson>().CheckSpawn();
                        default:
                            Debug.WriteLine("undecodable:" + s);
                            return null;
                    }
                })
                .Where(s => s != null);
        }
    }
}