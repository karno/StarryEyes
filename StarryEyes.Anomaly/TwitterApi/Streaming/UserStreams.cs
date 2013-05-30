using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class UserStreams
    {
        private const string EndpointUserStreams = "https://userstream.twitter.com/1.1/user.json";

        public static IObservable<string> ConnectUserStreams(
            this IOAuthCredential credential, IEnumerable<string> tracks)
        {
            return Observable.Create<string>(async (observer, cancel) =>
            {
                try
                {
                    // using GZip cause receiving elements delayed.
                    var client = credential.CreateOAuthClient(useGZip: false);
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                    using (var stream = await client.GetStreamAsync(EndpointUserStreams))
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream && !cancel.IsCancellationRequested)
                        {
                            var line = await reader.ReadLineAsync();
                            observer.OnNext(line);
                        }
                    }
                    if (!cancel.IsCancellationRequested)
                    {
                        observer.OnCompleted();
                    }
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });
        }
    }
}
