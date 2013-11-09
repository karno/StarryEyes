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
            if (element.text())
            {
                // standard status receiving
                handler.OnStatus(new TwitterStatus(element));
            }
            // element.foo() -> element.IsDefined("foo")
            if (element.delete())
            {
                // delete handler
                handler.OnDeleted(new StreamDelete
                {
                    Id = Int64.Parse(element.delete.status.id_str),
                    UserId = Int64.Parse(element.delete.status.user_id_str)
                });
                return;
            }
            if (element.scrub_geo())
            {
                // TODO: Not implemented.(Location deletion notices)
                return;
            }
            if (element.limit())
            {
                handler.OnTrackLimit(new StreamTrackLimit
                {
                    UndeliveredCount = (long)element.limit.track
                });
                return;
            }
            if (element.status_withheld() || element.user_withheld())
            {
                // TODO: Not implemented.(???)
                return;
            }
            if (element.disconnect())
            {
                handler.OnDisconnect(new StreamDisconnect
                {
                    Code = (DisconnectCode)element.disconnect.code,
                    Reason = element.disconnect.reason,
                    StreamName = element.disconnect.stream_name
                });
            }
            if (element.warning())
            {
                // TODO: Not implemented.(stall warning)
                return;
            }
            if (element.friends())
            {
                handler.OnEnumerationReceived(new StreamEnumeration
                {
                    Friends = (long[])element.friends
                });
            }
            if (element.IsDefined("event"))
            {
                string ev = ((string)element["event"]).ToLower();
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
                            CreatedAt = ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
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
                            CreatedAt = ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
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
                            CreatedAt = ((string)element.created_at).ParseDateTime(ParsingExtension.TwitterDateTimeFormat),
                        });
                        break;
                }
            }
        }
    }
}
