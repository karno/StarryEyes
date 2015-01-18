using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly.TwitterApi.Streaming
{
    public static class UserStreams
    {
        public static IObservable<string> ConnectUserStreams(
            this IOAuthCredential credential, IEnumerable<string> tracks,
            bool repliesAll = false, bool followingsActivity = false)
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
                {"include_followings_activity", followingsActivity ? "true" : null}
            }.ParametalizeForGet();
            return Observable.Create<string>((observer, cancel) => Task.Run(async () =>
            {
                HttpClient client = null;
                try
                {
                    // using GZip cause receiving elements delayed.
                    client = credential.CreateOAuthClient(useGZip: false);
                    // disable connection timeout due to streaming specification
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.MaxResponseContentBufferSize = 1024 * 16; // set buffer length as 16KB.
                    var endpoint = HttpUtility.ConcatUrl(ApiAccessProperties.UserStreamsEndpoint, "user.json");
                    if (!String.IsNullOrEmpty(param))
                    {
                        endpoint += "?" + param;
                    }
                    using (var stream = await client.GetStreamAsync(endpoint))
                    using (var reader = new StreamReader(stream))
                    {
                        try
                        {
                            // reader.EndOfStream 
                            while (!cancel.IsCancellationRequested)
                            {
                                var readLine = reader.ReadLineAsync();
                                var delay = Task.Delay(TimeSpan.FromSeconds(ApiAccessProperties.StreamingTimeoutSec),
                                    cancel);
                                if (await Task.WhenAny(readLine, delay) == delay)
                                {
                                    // timeout
                                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# TIMEOUT.");
                                    break;
                                }
                                var line = readLine.Result;
                                if (line == null)
                                {
                                    // connection closed
                                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                                    break;
                                }
                                if (!String.IsNullOrEmpty(line))
                                {
                                    // successfully completed
                                    observer.OnNext(line);
                                }
                            }
                        }
                        finally
                        {
                            // cancel pending requests
                            client.CancelPendingRequests();
                            client.Dispose();
                            client = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# error detected: " + ex.Message);
                    observer.OnError(ex);
                    return;
                }
                finally
                {
                    if (client != null)
                    {
                        client.CancelPendingRequests();
                        client.Dispose();
                    }
                }

                System.Diagnostics.Debug.WriteLine("#USERSTREAM# disconnection detected. (CANCELLATION REQUEST? " + cancel.IsCancellationRequested + ")");
                observer.OnCompleted();
            }, cancel));
        }
    }
}
