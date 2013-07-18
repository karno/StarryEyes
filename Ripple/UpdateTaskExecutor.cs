using System;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ripple
{
    public class UpdateTaskExecutor
    {
        public UpdateTaskExecutor(string pubkey, string basepath)
        {
            this.PublicKey = pubkey;
            this.BasePath = basepath;

        }

        public event Action<string> OnNotifyProgress;

        public string PublicKey { get; private set; }

        public string BasePath { get; private set; }

        public void NotifyProgress(string text, bool linefeed = true)
        {
            var handler = OnNotifyProgress;
            if (handler != null)
            {
                OnNotifyProgress(text + (linefeed ? Environment.NewLine : string.Empty));
            }
        }

        public async Task StartUpdate(CancellationToken ctoken)
        {
            this.NotifyProgress("Requesting update description...");
            var dlstr = await this.DownloadString("http://krile.starwing.net/update/update.xml", Encoding.UTF8);
            this.NotifyProgress("update xml: " + dlstr.Length + " bytes.");
            var dlbin = await this.DownloadBinary("http://krile.starwing.net/update/patches/2005.3.ksp");
            this.NotifyProgress("update bin: " + dlbin.Length + " bytes.");

            for (var i = 0; i < 100; i++)
            {
                if (i % 10 == 0)
                {
                    this.NotifyProgress(i + "%", false);
                }
                else if (i % 2 == 0)
                {
                    this.NotifyProgress(".", false);
                }
                Thread.Sleep(10);
            }
            this.NotifyProgress("complete.");
            this.NotifyProgress("determining package...");
            Thread.Sleep(1500);
            this.NotifyProgress("3 patches will be installed.");

            for (int p = 1; p < 4; p++)
            {
                this.NotifyProgress("downloading patch(" + p + "/3)...");
                for (var i = 0; i < 100; i++)
                {
                    if (i % 10 == 0)
                    {
                        this.NotifyProgress(i + "%", false);
                    }
                    else if (i % 2 == 0)
                    {
                        this.NotifyProgress(".", false);
                    }
                    Thread.Sleep(10);
                }
                this.NotifyProgress("complete.");
            }
            this.NotifyProgress("applying patches...");
            Thread.Sleep(1000);
            this.NotifyProgress("writing file: krile.exe");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: krile.db");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            Thread.Sleep(10);
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            this.NotifyProgress("writing file: StarryEyes.Anomaly.dll");
            Thread.Sleep(10);
            Thread.Sleep(10);
            Thread.Sleep(10);
            Thread.Sleep(10);
            this.NotifyProgress("complete!");
        }

        public async Task<string> DownloadString(string url, Encoding encoding)
        {
            return encoding.GetString(await this.DownloadBinary(url));
        }

        public async Task<byte[]> DownloadBinary(string url)
        {
            var progress = 0;
            var http = new HttpClientHandler();
            var pmh = new ProgressMessageHandler(http);
            pmh.HttpReceiveProgress += (sender, args) =>
            {
                if (progress == args.ProgressPercentage) return;
                progress = args.ProgressPercentage;
                if (progress % 10 == 0 && progress != 100)
                {
                    this.NotifyProgress(progress + "%", false);
                }
                else if (progress % 2 == 0)
                {
                    this.NotifyProgress(".", false);
                }
            };
            var client = new HttpClient(pmh);
            var result = await client.GetByteArrayAsync(url);
            this.NotifyProgress("complete.");
            return result;
        }
    }
}
