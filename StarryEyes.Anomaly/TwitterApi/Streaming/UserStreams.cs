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

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SEUTRACE");
            Directory.CreateDirectory(dir);
            var logFile = Path.Combine(dir, credential.OAuthAccessToken + ".log");

            var fstream = File.Open(logFile, FileMode.OpenOrCreate, FileAccess.Write);
            var writer = new StreamWriter(fstream);

            writer.WriteLine(DateTime.Now.ToString() + ": #CONOPEN");
            return Observable.Create<string>((observer, cancel) => Task.Run(async () =>
            {
                try
                {
                    // using GZip cause receiving elements delayed.
                    var client = credential.CreateOAuthClient(useGZip: false);
                    // disable connection timeout due to streaming specification
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                    var endpoint = EndpointUserStreams;
                    if (!String.IsNullOrEmpty(param))
                    {
                        endpoint += "?" + param;
                    }
                    using (var stream = await client.GetStreamAsync(endpoint))
                    using (var reader = new StreamReader(stream))
                    {
                        // reader.EndOfStream 
                        while (!cancel.IsCancellationRequested)
                        {
                            var readLine = reader.ReadLineAsync();
                            var delay = Task.Delay(TimeSpan.FromSeconds(ApiAccessProperties.StreamingTimeoutSec), cancel);
                            if (await Task.WhenAny(readLine, delay) == delay)
                            {
                                writer.WriteLine(DateTime.Now.ToString() + ": #TIMEOUT");
                                // timeout
                                System.Diagnostics.Debug.WriteLine("#USERSTREAM# TIMEOUT.");
                                break;
                            }
                            var line = readLine.Result;
                            if (line == null)
                            {
                                writer.WriteLine(DateTime.Now.ToString() + ": #CONCLOSE");
                                // connection closed
                                System.Diagnostics.Debug.WriteLine("#USERSTREAM# CONNECTION CLOSED.");
                                break;
                            }
                            if (String.IsNullOrEmpty(line))
                            {
                                writer.WriteLine(DateTime.Now.ToString() + ": RECEIVED EMPTY LINE");
                            }
                            else
                            {
                                writer.WriteLine(DateTime.Now.ToString() + ": RECEIVED DIGEST" + line.Substring(0, Math.Min(10, line.Length)));
                            }
                            if (!String.IsNullOrEmpty(line))
                            {
                                // successfully completed
                                observer.OnNext(line);
                            }
                            writer.Flush();
                        }
                        writer.WriteLine(DateTime.Now.ToString() + ": EXIT LOOP.");
                        writer.WriteLine(DateTime.Now.ToString() + ": CREQ?:" + cancel.IsCancellationRequested);
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    writer.WriteLine(DateTime.Now.ToString() + ": THROWN:" + ex.Message);
                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# error detected: " + ex.Message);
                    observer.OnError(ex);
                    return;
                }
                finally
                {
                    writer.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("#USERSTREAM# disconnection detected. (CANCELLATION REQUEST? " + cancel.IsCancellationRequested + ")");
                if (!cancel.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("#USERSTREAM# notify disconnection to upper layer.");
                    observer.OnCompleted();
                }
            }, cancel));
        }
    }
}
