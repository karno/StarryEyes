using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.Streams.Internals;

namespace StarryEyes.Anomaly.TwitterApi.Streams
{
    /// <summary>
    /// Core of engine of receiving general streams.
    /// </summary>
    internal static class StreamWinder
    {
        public static async Task Run([NotNull] Stream stream, [NotNull] Action<string> parser,
            TimeSpan readTimeout, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            // create timeout cancellation token source
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var reader = new CancellableStreamReader(stream))
            {
                var localToken = tokenSource.Token;
                while (true)
                {
                    localToken.ThrowIfCancellationRequested();

                    // set read timeout(add epsilon time to timeout (100 msec))
                    tokenSource.CancelAfter(readTimeout + TimeSpan.FromTicks(10000 * 100));

                    // execute reading next line
                    var line = (await reader.ReadLineAsync(localToken).ConfigureAwait(false));

                    // disable timer
                    tokenSource.CancelAfter(Timeout.InfiniteTimeSpan);

                    if (line == null)
                    {
                        System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                        break;
                    }

                    // skip empty response
                    if (String.IsNullOrWhiteSpace(line)) continue;

                    // call parser with read line
#pragma warning disable 4014
                    Task.Run(() => parser(line), cancellationToken);
#pragma warning restore  4014
                }
            }
        }
    }
}
