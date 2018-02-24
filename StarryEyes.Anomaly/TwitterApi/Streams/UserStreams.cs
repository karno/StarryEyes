using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cadena;
using JetBrains.Annotations;

namespace StarryEyes.Anomaly.TwitterApi.Streams
{
    public static class UserStreams
    {
        private const string EndpointUserStreams = "https://userstream.twitter.com/1.1/user.json";

        /// <summary>
        /// Connect to user streams.
        /// </summary>
        /// <param name="credential">API access preference</param>
        /// <param name="parser">Line handler</param>
        /// <param name="readTimeout">stream read timeout</param>
        /// <param name="cancellationToken">cancellation token object</param>
        /// <param name="tracksOrNull">tracks parameter(can be null)</param>
        /// <param name="repliesAll">repliesAll parameter</param>
        /// <param name="followingsActivity">include_followings_activity parameter</param>
        /// <returns></returns>
        public static async Task Connect([@CanBeNull] IOAuthCredential credential,
            [@CanBeNull] Action<string> parser, TimeSpan readTimeout, CancellationToken cancellationToken,
            [CanBeNullAttribute] IEnumerable<string> tracksOrNull = null, bool repliesAll = false,
            bool followingsActivity = false)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            // remove empty string and remove duplicates, concat strings
            var filteredTracks = tracksOrNull?.Select(t => t?.Trim())
                                              .Where(t => !String.IsNullOrEmpty(t))
                                              .Distinct()
                                              .JoinString(",");

            // bulid parameter
            var param = new Dictionary<string, object>
            {
                {"track", String.IsNullOrEmpty(filteredTracks) ? null : filteredTracks},
                {"replies", repliesAll ? "all" : null},
                {"include_followings_activity", followingsActivity ? "true" : null}
            }.ParametalizeForGet();
            var endpoint = EndpointUserStreams;
            if (!String.IsNullOrEmpty(param))
            {
                endpoint += "?" + param;
            }

            await Task.Run(async () =>
            {
                HttpClient client = null;
                try
                {
                    // prepare HttpClient
                    // GZip makes delay of delivery tweets
                    client = credential.CreateOAuthClient(useGZip: false);
                    // set parameters for receiving UserStreams.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    // begin connection
                    using (var resp = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false))
                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        // winding data from user stream
                        await StreamWinder.Run(stream, parser, readTimeout, cancellationToken).ConfigureAwait(false);
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