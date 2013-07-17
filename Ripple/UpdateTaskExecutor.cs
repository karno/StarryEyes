using System;
using System.Threading;

namespace Ripple
{
    public class UpdateTaskExecutor
    {
        public event Action<string> OnNotifyProgress;

        private void NotifyProgress(string text, bool linefeed = true)
        {
            var handler = OnNotifyProgress;
            if (handler != null)
            {
                OnNotifyProgress(text + (linefeed ? Environment.NewLine : string.Empty));
            }
        }

        public void StartUpdate()
        {
            this.NotifyProgress("Requesting update description...");
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
    }
}
