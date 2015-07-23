using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Artery.Streams;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class UserStreams
    {
        public static async Task ConnectUserStreams(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] IOldStreamHandler handler, TimeSpan readTimeout, CancellationToken cancellationToken,
            [CanBeNull] IEnumerable<string> tracksOrNull = null, bool repliesAll = false,
            bool followingsActivity = false)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (handler == null) throw new ArgumentNullException("handler");

            var filteredTracks =
                tracksOrNull != null
                    ? tracksOrNull.Where(t => !String.IsNullOrEmpty(t.Trim())).Distinct().JoinString(",")
                    : null;

            // bulid parameter
            var param = new Dictionary<string, object>
            {
                {"track", String.IsNullOrEmpty(filteredTracks) ? null : filteredTracks},
                {"replies", repliesAll ? "all" : null},
                {"include_followings_activity", followingsActivity ? "true" : null}
            }.ParametalizeForGet();
            var endpoint = HttpUtility.ConcatUrl(properties.Endpoint, "user.json");
            if (String.IsNullOrEmpty(param))
            {
                endpoint += "?" + param;
            }

            await Task.Run(async () =>
            {
                HttpClient client = null;
                try
                {
                    // prepare HttpClient
                    client = credential.CreateOAuthClient(useGZip: false);
                    // set parameters for receiving UserStreams.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.MaxResponseContentBufferSize = 1024 * 16;
                    // begin connection
                    using (var resp = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false))
                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        // run user stream engine
                        await UserStreamEngine.Run(stream, handler, readTimeout, cancellationToken)
                                              .ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (client != null)
                    {
                        // cancel pending requests
                        client.CancelPendingRequests();
                        client.Dispose();
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
