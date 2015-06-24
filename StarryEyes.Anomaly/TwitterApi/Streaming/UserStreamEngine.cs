using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    /// <summary>
    /// Streaming Engine Handler
    /// </summary>
    internal static class UserStreamEngine
    {
        public static async Task Run([NotNull] Stream stream, [NotNull] IStreamHandler handler,
            TimeSpan readTimeout, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (handler == null) throw new ArgumentNullException("handler");
            using (var reader = new CancellableStreamReader(stream))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    /* 
                     * NOTE: performance information
                     * Creating CancellationTokenSource each time is faster than using Task.Delay.
                     * Simpler way always defeats complex and confusing one.
                     */
                    // create timeout cancellation token source
                    using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        tokenSource.CancelAfter(readTimeout);
                        // execute read line
                        var line = (await reader.ReadLineAsync(tokenSource.Token).ConfigureAwait(false));
                        if (line == null)
                        {
                            System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                            break;
                        }

                        // skip empty response
                        if (String.IsNullOrWhiteSpace(line)) continue;

                        // read operation completed successfully
                        Task.Run(() => ParseStreamLine(line, handler), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private static void ParseStreamLine(string line, IStreamHandler handler)
        {
            var type = "initialize";
            try
            {
                var element = DynamicJson.Parse(line);

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
                    System.Diagnostics.Debug.WriteLine("!unknown event!:withheld" + Environment.NewLine +
                                                       element.ToString());
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
                    System.Diagnostics.Debug.WriteLine("!unknown event!:warning" + Environment.NewLine +
                                                       element.ToString());
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
                        case "favorited_retweet":
                            // TODO: unknown event. 
                            System.Diagnostics.Debug.WriteLine("!unknown event!:" + ev + Environment.NewLine +
                                                               element.ToString());
                            return;
                        default:
                            System.Diagnostics.Debug.WriteLine("!unknown event!:" + ev + Environment.NewLine +
                                                               element.ToString());
                            handler.OnExceptionThrownDuringParsing(new Exception("Unknown event: " + ev + " / " + element.ToString()));
                            return;
                    }
                }
                System.Diagnostics.Debug.WriteLine("!unknown element!" + Environment.NewLine + element.ToString());
                handler.OnExceptionThrownDuringParsing(new Exception("Unknown data: " + element.ToString()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("!exception thrown!" + Environment.NewLine + line);
                handler.OnExceptionThrownDuringParsing(new Exception("type:" + type, ex));
            }
        }
    }
}
