using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class UserStreams
    {
        private const string EndpointUserStreams = "https://userstream.twitter.com/1.1/user.json";

        public static IObservable<string> ConnectUserStreams(
            this IOAuthCredential credential, IEnumerable<string> tracks, bool repliesAll = false)
        {
            var filteredTracks = tracks != null
                                     ? tracks.Where(t => !String.IsNullOrEmpty(t))
                                             .Distinct()
                                             .JoinString(",")
                                     : null;
            var param = new Dictionary<string, object>
            {
                {"track", String.IsNullOrEmpty(filteredTracks) ? null : filteredTracks },
                {"replies", repliesAll ? "all" : null},
            }.ParametalizeForGet();
            return Observable.Create<string>((observer, cancel) => Task.Run(async () =>
            {
                try
                {
                    // using GZip cause receiving elements delayed.
                    var client = credential.CreateOAuthClient(useGZip: false);
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                    var endpoint = EndpointUserStreams;
                    if (!String.IsNullOrEmpty(param))
                    {
                        endpoint += "?" + param;
                    }
                    using (var stream = await client.GetStreamAsync(endpoint))
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream && !cancel.IsCancellationRequested)
                        {
                            var line = reader.ReadLine();
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
            }));
        }
    }
}
