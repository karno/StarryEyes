using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Artery.Streams.Notifications;
using StarryEyes.Anomaly.Artery.Streams.Notifications.Events;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Streaming;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.Artery.Streams
{
    /// <summary>
    /// Streaming Engine Handler
    /// </summary>
    internal static class UserStreamEngine
    {
        public static async Task Run([NotNull] Stream stream, [NotNull] IOldStreamHandler handler,
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

        private static void ParseStreamLine(string line, IOldStreamHandler handler)
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
                        handler.OnDeleted(new StreamDelete(
                            Int64.Parse(element.delete.status.id_str),
                            Int64.Parse(element.delete.status.user_id_str),
                            element.delete.timestamp_ms));
                    }
                    if (element.delete.direct_message())
                    {
                        // message is deleted
                        handler.OnDeleted(new StreamDelete(
                            Int64.Parse(element.delete.status.id_str),
                            // UserId = Int64.Parse(element.delete.status.user_id_str) // user_id_str field is not exist.
                            Int64.Parse(element.delete.direct_message.user_id.ToString()),
                            element.delete.timestamp_ms));
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
                    handler.OnTrackLimit(new StreamLimit((long)element.limit.track,
                        element.limit.timestamp_ms));
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
                    handler.OnDisconnect(new StreamDisconnect(
                        (DisconnectCode)element.disconnect.code,
                        element.disconnect.stream_name, element.disconnect.reason,
                         element.disconnect.timestamp_ms));
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
                    handler.OnEnumerationReceived(new StreamEnumeration((long[])element.friends));
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
                            handler.OnStatusActivity(new StreamStatusEvent(
                                new TwitterUser(element.source),
                                new TwitterUser(element.target),
                                new TwitterStatus(element.target_object), ev,
                                ((string)element.created_at).ParseTwitterDateTime()));
                            return;
                        case "block":
                        case "unblock":
                        case "follow":
                        case "unfollow":
                        case "user_update":
                            handler.OnUserActivity(new StreamUserEvent(
                                new TwitterUser(element.source),
                                new TwitterUser(element.target), ev,
                                ((string)element.created_at).ParseTwitterDateTime()));
                            return;
                        case "list_created":
                        case "list_destroyed":
                        case "list_updated":
                        case "list_member_added":
                        case "list_member_removed":
                        case "list_user_subscribed":
                        case "list_user_unsubscribed":
                            handler.OnListActivity(new StreamListEvent(
                                new TwitterUser(element.source), new TwitterUser(element.target),
                                new TwitterList(element.target_object), ev,
                                ((string)element.created_at).ParseTwitterDateTime()));
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
