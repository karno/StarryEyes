using System;
using System.Collections.Generic;
using StarryEyes.Anomaly.Artery.Streams.Notifications;
using StarryEyes.Anomaly.Artery.Streams.Notifications.Events;
using StarryEyes.Anomaly.Artery.Streams.Notifications.Warnings;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.Artery.Streams.Internal
{
    internal static class TwitterStreamParser
    {
        /// <summary>
        /// Parse streamed JSON line
        /// </summary>
        /// <param name="line">JSON line</param>
        /// <param name="handler">result handler</param>
        public static void ParseStreamLine(string line, IStreamHandler handler)
        {
            try
            {
                var element = DynamicJson.Parse(line);
                if (!ParseStreamLineAsStatus(element, handler))
                {
                    ParseNotStatusStreamLine(element, handler);
                }
            }
            catch (Exception ex)
            {
                handler.OnException(new StreamParseException(
                    "JSON parse failed.", line, ex));
            }
        }

        /// <summary>
        /// Check parse streamed JSON line as normal (not direct-message) status
        /// </summary>
        /// <param name="graph">JSON object graph</param>
        /// <param name="handler">stream handler</param>
        /// <returns></returns>
        internal static bool ParseStreamLineAsStatus(dynamic graph, IStreamHandler handler)
        {
            if (!graph.text()) return false;
            handler.OnStatus(new TwitterStatus(graph));
            return true;
        }

        /// <summary>
        /// Parse streamed JSON line (which is not a status)
        /// </summary>
        /// <param name="graph">JSON object graph</param>
        /// <param name="handler">result handler</param>
        internal static void ParseNotStatusStreamLine(dynamic graph, IStreamHandler handler)
        {
            try
            {
                // element.foo() -> element.IsDefined("foo")

                // direct message
                if (graph.direct_message())
                {
                    handler.OnStatus(new TwitterStatus(graph.direct_message));
                    return;
                }

                // delete
                if (graph.delete())
                {
                    if (graph.delete.status())
                    {
                        handler.OnNotification(new StreamDelete(
                            Int64.Parse(graph.delete.status.id_str),
                            Int64.Parse(graph.delete.status.user_id_str),
                            graph.delete.timestamp_ms));
                        return;
                    }
                    if (graph.delete.direct_message())
                    {
                        handler.OnNotification(new StreamDelete(
                            Int64.Parse(graph.delete.status.id_str),
                            Int64.Parse(graph.delete.direct_message.user_id.ToString()),
                            graph.delete.timestamp_ms));
                        return;
                    }
                }

                // scrub_geo
                if (graph.scrub_geo())
                {
                    handler.OnNotification(new StreamScrubGeo(
                        Int64.Parse(graph.scrub_geo.user_id_str),
                        Int64.Parse(graph.scrub_geo.up_to_status_id_str),
                        graph.scrub_geo.timestamp_ms));
                    return;
                }

                // limit
                if (graph.limit())
                {
                    handler.OnNotification(new StreamLimit(
                        (long)graph.limit.track,
                        graph.limit.timestamp_ms));
                    return;
                }

                // withheld
                if (graph.status_withheld())
                {
                    handler.OnNotification(new StreamWithheld(
                        Int64.Parse(graph.status_withheld.user_id),
                        Int64.Parse(graph.status_withheld.id),
                        graph.status_withheld.withheld_in_countries,
                        graph.status_withheld.timestamp_ms));
                    return;
                }
                if (graph.user_withheld())
                {
                    handler.OnNotification(new StreamWithheld(
                        Int64.Parse(graph.user_withheld.id),
                        graph.user_withheld.withheld_in_countries,
                        graph.user_withheld.timestamp_ms));
                    return;
                }

                // disconnect
                if (graph.disconnect())
                {
                    handler.OnNotification(new StreamDisconnect(
                        (DisconnectCode)graph.disconnect.code,
                        graph.disconnect.stream_name, graph.disconnect.reason,
                        graph.disconnect.timestamp_ms));
                    return;
                }

                // stall warning
                if (graph.warning())
                {
                    if (graph.warning.code == "FALLING_BEHIND")
                    {
                        handler.OnNotification(new StreamStallWarning(
                            graph.warning.code,
                            graph.warning.message,
                            graph.warning.percent_full,
                            graph.warning.timestamp_ms));
                        return;
                    }
                }

                // user update
                if (graph.IsDefined("event")) // 'event' is the reserved word...
                {
                    var ev = ((string)graph["event"]).ToLower();
                    if (ev == "user_update")
                    {
                        // parse user_update only in generic streams.
                        handler.OnNotification(new StreamUserEvent(
                            new TwitterUser(graph.source),
                            new TwitterUser(graph.target), ev,
                            ((string)graph.created_at).ParseTwitterDateTime()));
                        return;
                    }
                    // unknown event...
                    handler.OnNotification(new StreamUnknownNotification("event: " + ev, graph.ToString()));
                }

                if (graph.IsObject())
                {
                    // unknown...
                    foreach (KeyValuePair<string, dynamic> item in graph)
                    {
                        handler.OnNotification(new StreamUnknownNotification(item.Key, item.Value.ToString()));
                        return;
                    }
                }
                // unknown event-type...
                handler.OnNotification(new StreamUnknownNotification(null, graph.Value.ToString()));

            }
            catch (Exception ex)
            {
                handler.OnException(new StreamParseException(
                    "Stream graph parse failed.", graph.ToString(), ex));
            }
        }

    }
}
