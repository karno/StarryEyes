using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SweetMagic
{
    public class UpdateTaskExecutor
    {
        private readonly Version _version;
        private readonly int _processId;
        private readonly bool _acceptPreview;

        public UpdateTaskExecutor(Version version, string pubkey, string basepath, int processId, bool acceptPreview)
        {
            _version = version;
            this._processId = processId;
            _acceptPreview = acceptPreview;
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

        public async Task<bool> StartUpdate()
        {
            this.NotifyProgress("Requesting update patch definition...");
            var dlstr = await this.DownloadString("http://krile.starwing.net/shared/update.xml", Encoding.UTF8);
            this.NotifyProgress("patch definition: " + dlstr.Length + " bytes.");
            this.NotifyProgress("loading definition...", false);
            var pack = ReleasePack.Parse(dlstr);
            this.NotifyProgress("loaded.(patch stamp: " + pack.Timestamp.ToString("yyyy/MM/dd") + ")");
            var apply = pack.GetPatchesShouldBeApplied(_version, _acceptPreview).ToArray();
            this.NotifyProgress(apply.Length + " patches should be applied.");
            Thread.Sleep(10);
            // find process and kill
            try
            {
                var process = Process.GetProcessById(this._processId);
                this.NotifyProgress("waiting process in 10 sec...");
                process.WaitForExit(10000);
                if (!process.HasExited)
                {
                    this.NotifyProgress("killing process...", false);
                    process.Kill();
                    this.NotifyProgress("ok, continue...");
                }
            }
            catch (ArgumentException)
            {
                // process has exited.
            }
            foreach (var patch in apply)
            {
                var vs = patch.Version.ToString();
                if (patch.Version.Revision == 0)
                {
                    vs = patch.Version.ToString(3);
                }
                this.NotifyProgress("applying patch: v" + vs + " - " + patch.ReleaseTime.ToString("yyyy/MM/dd"));
                foreach (var action in patch.Actions)
                {
                    try
                    {
                        await action.DoWork(this);
                    }
                    catch (UpdateException uex)
                    {
                        this.NotifyProgress("*** FATAL: UpdateException raised. ***");
                        this.NotifyProgress(uex.Message);
                        MessageBox.Show(uex.Message + Environment.NewLine +
                                        "Please download and apply latest version from official website.",
                                        "Auto Update Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        this.NotifyProgress("update failed.");
                        Thread.Sleep(100);
                        return false;
                    }
                }
            }
            Thread.Sleep(10);
            this.NotifyProgress("update completed!");
            this.NotifyProgress("starting new binary, wait a moment...");
            Thread.Sleep(100);
            return true;
        }

        public async Task<string> DownloadString(string url, Encoding encoding)
        {
            return encoding.GetString(await this.DownloadBinary(url));
        }

        public async Task<byte[]> DownloadBinary(string url)
        {
            try
            {
                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                var result = await client.GetByteArrayAsync(url);
                this.NotifyProgress("download complete.");
                return result;
            }
            catch (Exception ex)
            {
                PrintExceptionSub(String.Empty, ex);
                return null;
            }
        }

        private void PrintExceptionSub(string prefix, Exception ex)
        {
            var pp = String.IsNullOrEmpty(prefix) ? "" : prefix + " ";
            var aex = ex as AggregateException;
            if (aex != null)
            {
                this.NotifyProgress(pp + "[Aggregated Exception]");
                foreach (var iex in aex.InnerExceptions)
                {
                    PrintExceptionSub(prefix + " |", iex);
                }
            }
            else
            {
                this.NotifyProgress(pp + ex.Message);
                if (ex.InnerException != null)
                {
                    PrintExceptionSub(prefix + ">", ex.InnerException);
                }
            }
        }
    }
}
