using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class StreamingConnector
    {
        public static IDisposable SubscribeWithHandler(this IObservable<string> streamElements,
                                                        IStreamHandler handler, Action<Exception> onError = null,
                                                        Action onCompleted = null)
        {
            if (streamElements == null) throw new ArgumentNullException("streamElements");
            if (handler == null) throw new ArgumentNullException("handler");
            return streamElements
                .Where(s => !String.IsNullOrWhiteSpace(s))
                .ObserveOn(TaskPoolScheduler.Default)
                .Select(s => DynamicJson.Parse(s))
                .Subscribe(
                    s => DispatchStreamingElements(s, handler),
                    onError ?? (ex => { }),
                    onCompleted ?? (() => { }));
        }

        private static void DispatchStreamingElements(dynamic element, IStreamHandler handler)
        {
            var type = "initialize";
            try
            {
                // element.foo() -> element.IsDefined("foo")
                if (element.text())
                {
                    // standard status receiving
                    type = "status";
                    handler.OnStatus(new TwitterStatus(element));
                    return;
                }
                if (element.direct_message())
                {
                    // direct message
                    type = "message";
                    handler.OnStatus(new TwitterStatus(element.direct_message));
                    return;
                }
                if (element.delete())
                {
                    type = "delete";
                    // status or message is deleted
                    if (element.delete.status())
                    {
                        // status is deleted
                        handler.OnDeleted(new StreamDelete
                        {
                            Id = Int64.Parse(element.delete.status.id_str),
                            UserId = Int64.Parse(element.delete.status.user_id_str)
                        });
                    }
                    if (element.delete.direct_message())
                    {
                        // message is deleted
                        handler.OnDeleted(new StreamDelete
                        {
                            Id = Int64.Parse(element.delete.direct_message.id_str),
                            // UserId = Int64.Parse(element.delete.status.user_id_str) // user_id_str field is not exist.
                            UserId = Int64.Parse(element.delete.direct_message.user_id.ToString())
                        });
                    }
                    return;
                }
                if (element.scrub_geo())
                {
                    type = "geolocation";
                    // TODO: Not implemented.(Location deletion notices)
                    return;
                }
                if (element.limit())
                {
                    type = "tracklimit";
                    handler.OnTrackLimit(new StreamTrackLimit
                    {
                        UndeliveredCount = (long)element.limit.track
                    });
                    return;
                }
                if (element.status_withheld() || element.user_withheld())
                {
                    type = "withheld";
                    // TODO: Not implemented.(???)
                    return;
                }
                if (element.disconnect())
                {
                    type = "discon";
                    handler.OnDisconnect(new StreamDisconnect
                    {
                        Code = (DisconnectCode)element.disconnect.code,
                        Reason = element.disconnect.reason,
                        StreamName = element.disconnect.stream_name
                    });
                    return;
                }
                if (element.warning())
                {
                    type = "warning";
                    // TODO: Not implemented.(stall warning)
                    return;
                }
                if (element.friends())
                {
                    type = "friends";
                    handler.OnEnumerationReceived(new StreamEnumeration
                    {
                        Friends = (long[])element.friends
                    });
                    return;
                }
                if (element.IsDefined("event"))
                {
                    type = "event";
                    string ev = ((string)element["event"]).ToLower();
                    type = "event:" + ev;
                    switch (ev)
                    {
                        case "favorite":
                        case "unfavorite":
                            handler.OnStatusActivity(new StreamStatusActivity
                            {
                                Target = new TwitterUser(element.target),
                                Source = new TwitterUser(element.source),
                                Event = StreamStatusActivity.ToEnumEvent(ev),
                                EventRawString = ev,
                                Status = new TwitterStatus(element.target_object),
                                CreatedAt =
                                    ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
                            });
                            return;
                        case "block":
                        case "unblock":
                        case "follow":
                        case "unfollow":
                        case "user_update":
                            handler.OnUserActivity(new StreamUserActivity
                            {
                                Target = new TwitterUser(element.target),
                                Source = new TwitterUser(element.source),
                                Event = StreamUserActivity.ToEnumEvent(ev),
                                EventRawString = ev,
                                CreatedAt =
                                    ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
                            });
                            return;
                        case "list_created":
                        case "list_destroyed":
                        case "list_updated":
                        case "list_member_added":
                        case "list_member_removed":
                        case "list_user_subscribed":
                        case "list_user_unsubscribed":
                            handler.OnListActivity(new StreamListActivity
                            {
                                Target = new TwitterUser(element.target),
                                Source = new TwitterUser(element.source),
                                Event = StreamListActivity.ToEnumEvent(ev),
                                EventRawString = ev,
                                List = new TwitterList(element.target_object),
                                CreatedAt =
                                    ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
                            });
                            return;
                        default:
                            handler.OnExceptionThrownDuringParsing(new Exception("Unknown event: " + ev + " / " + element.ToString()));
                            return;
                    }
                }
                handler.OnExceptionThrownDuringParsing(new Exception("Unknown data: " + element.ToString()));
            }
            catch (Exception ex)
            {
                string elemstr = element.ToString();
                System.Diagnostics.Debug.WriteLine("!exception thrown!" + Environment.NewLine + elemstr);
                handler.OnExceptionThrownDuringParsing(new Exception("type:" + type, ex));
            }
        }
    }
}
