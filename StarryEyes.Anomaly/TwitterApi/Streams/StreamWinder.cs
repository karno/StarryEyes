using System;
using System.Collections.Concurrent;
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
            var parseCollection = new BlockingCollection<string>();

            // create timeout cancellation token source
            try
            {
                using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                using (var reader = new CancellableStreamReader(stream))
                {
                    var localToken = tokenSource.Token;
                    // start parser worker thread
                    StartParserWorker(parseCollection, parser, localToken);

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
                        parseCollection.Add(line, localToken);
                    }
                }
            }
            finally
            {
                parseCollection.CompleteAdding();
            }
        }

        private static void StartParserWorker([NotNull] BlockingCollection<string> collection,
            [NotNull] Action<string> parser, CancellationToken token)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            const TaskCreationOptions option = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (!collection.IsCompleted && !token.IsCancellationRequested)
                    {
                        string item;
                        try
                        {
                            item = collection.Take();
                        }
                        catch (InvalidOperationException)
                        {
                            // BlockingCollection is completed.
                            break;
                        }
                        parser(item);
                    }
                }
                finally
                {
                    collection.Dispose();
                }
            }, token, option, TaskScheduler.Default);
        }
    }
}
