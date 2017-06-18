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
using StarryEyes.Albireo.Helpers;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;
using TaskDialogInterop;
using Application = System.Windows.Application;
using TaskDialog = StarryEyes.Nightmare.Windows.TaskDialog;
using TaskDialogCommonButtons = StarryEyes.Nightmare.Windows.TaskDialogCommonButtons;
using TaskDialogOptions = StarryEyes.Nightmare.Windows.TaskDialogOptions;
using VistaTaskDialogIcon = StarryEyes.Nightmare.Windows.VistaTaskDialogIcon;

namespace StarryEyes.Models.Subsystems
{
    public static class AutoUpdateService
    {
        private static bool _isUpdateNotified = false;

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
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    SubsystemResources.FailedCheckingUpdate, ex));
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
                    catch
                    {
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    SubsystemResources.FailedPrepareUpdate, ex));
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
                    Title = SubsystemResources.AutoUpdateFailedTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SubsystemResources.AutoUpdateFailedInst,
                    Content = SubsystemResources.AutoUpdateFailedContent,
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
                    Title = SubsystemResources.UpdateCompleteErrorTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SubsystemResources.UpdateCompleteErrorInst,
                    Content = SubsystemResources.UpdateCompleteErrorContent,
                    ExpandedInfo = SubsystemResources.UpdateCompleteErrorExInfo,
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

        internal static void StartSchedule(bool initial = true)
        {
            var rand = new Random(Environment.TickCount);
            // first startup -> 36 seconds later
            var next = initial ? 0.01 : 3 + 6 * rand.NextDouble();
            Observable
                .Timer(TimeSpan.FromHours(next))
                .Subscribe(async _ =>
                {
                    if (await CheckPrepareUpdate(App.Version))
                    {
                        if (_isUpdateNotified) return;
                        _isUpdateNotified = true;
                        var option = new TaskDialogOptions
                        {
                            Title = SubsystemResources.UpdateAvailableTitle,
                            MainIcon = VistaTaskDialogIcon.Information,
                            MainInstruction = SubsystemResources.UpdateAvailableInst,
                            Content = SubsystemResources.UpdateAvailableContent,
                            CustomButtons = new[]
                            {
                                SubsystemResources.UpdateAvailableButtonImmediate,
                                SubsystemResources.UpdateAvailableButtonLater
                            },
                            Callback = (dialog, args, data) =>
                            {
                                if (args.Notification == VistaTaskDialogNotification.ButtonClicked &&
                                    args.ButtonIndex == 0)
                                {
                                    // update immediately
                                    StartUpdate(App.Version);

                                }
                                return false;
                            }
                        };
                        MainWindowModel.ShowTaskDialog(option);
                    }
                    else
                    {
                        StartSchedule(false);
                    }
                });
        }
    }
}
