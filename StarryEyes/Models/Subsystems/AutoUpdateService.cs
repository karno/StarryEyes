using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using StarryEyes.Albireo;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using Application = System.Windows.Application;

namespace StarryEyes.Models.Subsystems
{
    public static class AutoUpdateService
    {
        private static string _patcherUri;
        private static string _patcherSignUri;

        public static event Action UpdateStateChanged;

        private static string ExecutablePath
        {
            get { return Path.Combine(App.LocalUpdateStorePath, App.UpdaterFileName); }
        }

        private static string PostUpdateFilePath
        {
            get { return Path.Combine(App.LocalUpdateStorePath, App.PostUpdateFileName); }
        }

        public static bool IsUpdateBinaryExisted()
        {
            return File.Exists(ExecutablePath);
        }

        public static bool IsPostUpdateFileExisted()
        {
            return File.Exists(PostUpdateFilePath);
        }

        private static async Task<bool> CheckUpdate(Version version)
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
                if (xdoc.Root == null)
                {
                    throw new Exception("could not read definition xml.");
                }
                _patcherUri = xdoc.Root.Attribute("patcher").Value;
                _patcherSignUri = xdoc.Root.Attribute("sign").Value;
                var releases = xdoc.Root.Descendants("release");
                var latest = releases.Select(r => Version.Parse(r.Attribute("version").Value))
                                     .Where(v => Setting.AcceptUnstableVersion.Value || v.Revision <= 0)
                                     .OrderByDescending(v => v)
                                     .FirstOrDefault();
                if (version != null && latest > version)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(
                    new OperationFailedEvent("更新の確認に失敗しました", ex));
            }
            return false;
        }

        internal static async Task<bool> CheckPrepareUpdate(Version version)
        {
            if (!await CheckUpdate(version))
            {
                return false;
            }
            if (String.IsNullOrEmpty(_patcherUri) || String.IsNullOrEmpty(_patcherSignUri))
            {
                return false;
            }
            try
            {
                if (IsUpdateBinaryExisted() || Directory.Exists(App.LocalUpdateStorePath))
                {
                    // files are already downloaded.
                    return true;
                }
                try
                {
                    Directory.CreateDirectory(App.LocalUpdateStorePath);
                    using (var http = new HttpClient())
                    {
                        var patcher = await http.GetByteArrayAsync(_patcherUri);
                        var patcherSign = await http.GetByteArrayAsync(_patcherSignUri);
                        var pubkey = File.ReadAllText(App.PublicKeyFile);
                        if (!VerifySignature(patcher, patcherSign, pubkey))
                        {
                            throw new Exception("Updater signature is invalid.");
                        }
                        File.WriteAllBytes(ExecutablePath, patcher);
                    }
                    UpdateStateChanged.SafeInvoke();
                    return true;
                }
                catch
                {
                    try
                    {
                        Directory.Delete(App.LocalUpdateStorePath, true);
                    }
                    catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("自動更新の準備に失敗しました", ex));
                return false;
            }
        }

        internal static void StartUpdate(Version version)
        {
            try
            {
                var ver = "0.0.0";
                if (version != null)
                {
                    ver = version.ToString(3);
                    if (Setting.IsLoaded && Setting.AcceptUnstableVersion.Value)
                    {
                        ver = App.Version.ToString(4);
                    }
                }
                var pubkey = Path.Combine(App.ExeFileDir, App.PublicKeyFile);
                var dest = App.ExeFileDir;
                var pid = Process.GetCurrentProcess().Id;
                var args = new[]
                {
                    ver,
                    pubkey,
                    dest,
                    pid.ToString(CultureInfo.InvariantCulture)
                }.Select(s => "\"" + s + "\"").JoinString(" ");
                var startInfo = new ProcessStartInfo(ExecutablePath)
                {
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = App.LocalUpdateStorePath,
                };
                MainWindowModel.SuppressCloseConfirmation = true;
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "自動アップデート エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "アップデートを開始できませんでした。",
                    Content = "Krileを再起動してやり直すか、手動で最新版を入手してください。",
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close
                });
                // cleanup update binaries
                try
                {
                    if (Directory.Exists(App.LocalUpdateStorePath))
                    {
                        Directory.Delete(App.LocalUpdateStorePath, true);
                    }
                }
                catch
                {
                }
            }
        }

        internal static void PostUpdate()
        {
            var retryCount = 0;
            try
            {
                var directory = new DirectoryInfo(App.LocalUpdateStorePath);
                while (directory.Exists)
                {
                    try
                    {
                        // remove "read-only" attribute
                        directory.GetFiles("*", SearchOption.AllDirectories)
                            .Where(file => file.Attributes.HasFlag(FileAttributes.ReadOnly))
                            .ForEach(f => f.Attributes ^= FileAttributes.ReadOnly);

                        // delete directory
                        directory.Delete(true);
                        break;
                    }
                    catch (Exception)
                    {
                        if (retryCount > 10)
                        {
                            // exit loop
                            throw;
                        }
                        Thread.Sleep(1000);
                        // refresh directory state
                        directory.Refresh();
                        retryCount++;
                    }
                }
            }
            catch (Exception)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "アップデート完了エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "更新バイナリを削除できません。",
                    Content = "ユーザーデータディレクトリに存在するupdateフォルダを手動で削除してください。",
                    ExpandedInfo = "ユーザーデータディレクトリの位置については、FAQを参照してください。" + Environment.NewLine +
                                   "削除出来ない場合は、Windowsを再起動する必要があるかもしれません。",
                    CommonButtons = TaskDialogCommonButtons.Close
                });
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
            var rand = new Random(Environment.TickCount);
            var next = 3 + 6 * rand.NextDouble();
            Observable.Timer(TimeSpan.FromHours(next))
                      .Subscribe(async _ =>
                      {
                          if (await CheckPrepareUpdate(App.Version))
                          {
                              BackstageModel.RegisterEvent(new UpdateAvailableEvent());
                          }
                          StartSchedule();
                      });
        }
    }
}
