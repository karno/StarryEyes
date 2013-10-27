using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Nightmare.Windows;
using Application = System.Windows.Application;

namespace StarryEyes.Models.Subsystems
{
    public static class AutoUpdateService
    {
        private static string patcherUri = null;
        private static string patcherSignUri = null;

        private static string ExecutablePath
        {
            get { return Path.Combine(App.LocalUpdateStorePath, App.UpdaterFileName); }
        }

        public static bool IsUpdateBinaryExisted()
        {
            return File.Exists(ExecutablePath);
        }

        public static async Task<bool> CheckUpdate()
        {
            if (IsUpdateBinaryExisted()) return true;
            try
            {
                string verXml;
                using (var http = new HttpClient())
                {
                    verXml = await http.GetStringAsync(App.RemoteVersionXml);
                }
                var xdoc = XDocument.Load(new StringReader(verXml));
                patcherUri = xdoc.Root.Attribute("patcher").Value;
                patcherSignUri = xdoc.Root.Attribute("sign").Value;
                var releases = xdoc.Root.Descendants("release");
                var latest = releases.Select(r => double.Parse(r.Attribute("version").Value))
                                     .OrderByDescending(v => v)
                                     .FirstOrDefault();
                if (latest > App.NumericVersion)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent("Update check failed: " + ex.Message));
            }
            return false;
        }

        public static async Task<bool> PrepareUpdate()
        {
            if (String.IsNullOrEmpty(patcherUri) || String.IsNullOrEmpty(patcherSignUri))
            {
                if (!await CheckUpdate())
                {
                    return false;
                }
            }
            try
            {
                Directory.CreateDirectory(App.LocalUpdateStorePath);
                using (var http = new HttpClient())
                {
                    var patcher = await http.GetByteArrayAsync(patcherUri);
                    var patcherSign = await http.GetByteArrayAsync(patcherSignUri);
                    var pubkey = File.ReadAllText(App.PublicKeyFile);
                    if (!VerifySignature(patcher, patcherSign, pubkey))
                    {
                        throw new Exception("Updater signature invalid.");
                    }
                    File.WriteAllBytes(ExecutablePath, patcher);
                }
                return true;
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("Auto update preparation failed: " + ex.Message));
                return false;
            }
        }

        public static void StartUpdate()
        {
            try
            {
                var ver = App.NumericVersion.ToString("0.00");
                var pubkey = Path.Combine(App.ExeFileDir, App.PublicKeyFile);
                var dest = App.ExeFileDir;
                var pid = Process.GetCurrentProcess().Id;
                var args = new[]
                {
                    ver,
                    pubkey,
                    dest,
                    pid.ToString()
                }.Select(s => "\"" + s + "\"").JoinString(" ");
                var startInfo = new ProcessStartInfo(ExecutablePath)
                {
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = App.LocalUpdateStorePath,
                };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "アップデートを開始できませんでした。",
                    Content = "Krileを再起動してやり直すか、手動で最新版を入手してください。",
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close
                });
            }
        }

        public static void PostUpdate()
        {
            var retryCount = 0;
            while (Directory.Exists(App.LocalUpdateStorePath))
            {
                try
                {
                    Directory.Delete(App.LocalUpdateStorePath, true);
                }
                catch (IOException)
                {
                    if (retryCount > 10)
                    {
                        throw;
                    }
                    Thread.Sleep(1000);
                    retryCount++;
                }
            }
        }

        private static bool VerifySignature(byte[] bytes, byte[] signature, String publicKey)
        {
            using (var sha = new SHA256Managed())
            using (var rsa = new RSACryptoServiceProvider())
            {
                // Compute hash
                var hash = sha.ComputeHash(bytes);
                // RSA Initialize
                rsa.FromXmlString(publicKey);
                // format
                var deformatter = new RSAPKCS1SignatureDeformatter(rsa);
                deformatter.SetHashAlgorithm("SHA256");
                return deformatter.VerifySignature(hash, signature);
            }
        }

        public static void StartSchedule()
        {
            Task.Run(async () =>
            {
                if (await CheckUpdate() && await PrepareUpdate())
                {
                    BackstageModel.RegisterEvent(new UpdateAvailableEvent());
                }
            });
            var rand = new Random(Environment.TickCount);
            var next = 3 + 6 * rand.NextDouble();
            Observable.Timer(TimeSpan.FromHours(next))
                      .Subscribe(async _ =>
                      {
                          if (await CheckUpdate() && await PrepareUpdate())
                          {
                              BackstageModel.RegisterEvent(new UpdateAvailableEvent());
                          }
                          StartSchedule();
                      });
        }
    }
}
