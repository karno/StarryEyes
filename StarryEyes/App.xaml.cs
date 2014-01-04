using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Livet;
using StarryEyes.Annotations;
using StarryEyes.Casket;
using StarryEyes.Models;
using StarryEyes.Models.Plugins;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Plugins;
using StarryEyes.Settings;
using StarryEyes.Views.Dialogs;

namespace StarryEyes
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App
    {
        private static readonly string DbVersion = "1.0";
        private static Mutex _appMutex;
        private static DateTime _startupTime;

        /// <summary>
        /// Application entry point
        /// </summary>
        private void AppStartup(object sender, StartupEventArgs e)
        {
            _startupTime = DateTime.Now;

            #region initialize configuration directory

            // create data-store directory
            try
            {
                Directory.CreateDirectory(ConfigurationDirectoryPath);
            }
            catch (Exception ex)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "Krile StarryEyes",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "Krileの起動に失敗しました。",
                    Content = "設定を保持するディレクトリを作成できません。",
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = "別の場所への配置を試みてください。"
                });
                Environment.Exit(-1);
            }

            // enable multi-core JIT.
            // see reference: http://msdn.microsoft.com/en-us/library/system.runtime.profileoptimization.aspx
            if (IsMulticoreJitEnabled && !(e.Args.Select(a => a.ToLower()).Contains("-maintenance")))
            {
                ProfileOptimization.SetProfileRoot(ConfigurationDirectoryPath);
                ProfileOptimization.StartProfile(ProfileFileName);
            }

            // initialize dispatcher helper
            DispatcherHelper.UIDispatcher = Dispatcher;
            DispatcherHolder.Initialize(Dispatcher);

            // set rendering mode
            if (!IsHardwareRenderingEnabled)
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }

            #endregion

            #region detect run duplication

            // Check run duplication
            string mutexStr = null;
            switch (ExecutionMode)
            {
                case ExecutionMode.Default:
                case ExecutionMode.Roaming:
                    mutexStr = ExecutionMode.ToString();
                    break;
                case ExecutionMode.Standalone:
                    mutexStr = "Standalone_" + ExeFilePath.Replace('\\', '*');
                    break;
            }
            _appMutex = new Mutex(true, "Krile_StarryEyes_" + mutexStr);

            if (_appMutex.WaitOne(0, false) == false)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "Krile StarryEyes",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "Krileはすでに起動しています。",
                    Content = "同じ設定を共有するKrileを多重起動することはできません。",
                    ExpandedInfo = "Krileを多重起動するためには、krile.exe.configを編集する必要があります。" + Environment.NewLine +
                    "詳しくは公式ウェブサイト上のFAQを参照してください。",
                    CommonButtons = TaskDialogCommonButtons.Close
                });
                Environment.Exit(0);
            }

            #endregion

            // set exception handlers
            Current.DispatcherUnhandledException += (sender2, e2) => HandleException(e2.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender2, e2) => HandleException(e2.ExceptionObject as Exception);

            #region clean up update binary
            if (e.Args.Select(a => a.ToLower()).Contains("-postupdate"))
            {
                // remove kup.exe
                AutoUpdateService.PostUpdate();
            }

            #endregion

            #region check and show pre-execute dialog

            if (e.Args.Select(a => a.ToLower()).Contains("-maintenance"))
            {
                if (!this.ShowPreExecuteDialog())
                {
                    Environment.Exit(0);
                }
            }

            #endregion

            #region register core handlers

            // set exception handlers
            Current.DispatcherUnhandledException += (sender2, e2) => HandleException(e2.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender2, e2) => HandleException(e2.ExceptionObject as Exception);

            // set exit handler
            Current.Exit += (_, __) => AppFinalize(true);

            #endregion

            #region initialize web connection parameters

            // initialize service points
            ServicePointManager.Expect100Continue = false; // disable expect 100 continue for User Streams connection.
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue; // Limit Break!

            // declare security protocol explicitly
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

            // initialize anomaly core system
            Anomaly.Core.Initialize();

            #endregion

            // initialize special image handlers
            SpecialImageResolvers.Initialize();

            // load plugins
            PluginManager.Load(Path.Combine(ExeFileDir, PluginDirectory));

            // load settings
            if (!Setting.LoadSettings())
            {
                // fail loading settings
                Current.Shutdown();
                Environment.Exit(0);
            }

            if (Setting.DatabaseErrorOccured.Value)
            {
                var result = TaskDialog.Show(new TaskDialogOptions
                {
                    Title = "データベース エラーの検出",
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = "データベースでエラーが検出されています。",
                    Content = "データベースを初期化するまでエラーが継続することがあります。" + Environment.NewLine +
                              "何度起動しても落ちてしまう場合、データベースを削除すると改善することがあります。。" + Environment.NewLine +
                              "データベースを初期化しますか？",
                    CommonButtons = TaskDialogCommonButtons.YesNo,
                    DefaultButtonIndex = 1
                });
                if (result.Result == TaskDialogSimpleResult.Yes)
                {
                    if (File.Exists(DatabaseFilePath))
                    {
                        File.Delete(DatabaseFilePath);
                    }
                }
                Setting.DatabaseErrorOccured.Value = false;
            }

            #region Execute update

            // requires settings
            if (AutoUpdateService.IsUpdateBinaryExisted())
            {
                // execute update
                AutoUpdateService.StartUpdate(App.Version);
                Environment.Exit(0);
            }

            #endregion

            // set parameters for accessing twitter.
            Networking.Initialize();

            // load themes
            ThemeManager.Initialize();

            // load key assigns
            KeyAssignManager.Initialize();

            // load cache manager
            CacheStore.Initialize();

            // initialize stores
            Database.Initialize(DatabaseFilePath);
            if (!this.CheckDatabase())
            {
                // db migration failed
                Current.Shutdown();
                Environment.Exit(0);
            }

            // initialize subsystems
            StatisticsService.Initialize();
            PostLimitPredictionService.Initialize();
            MuteBlockManager.Initialize();
            StatusBroadcaster.Initialize();
            StatusInbox.Initialize();
            AutoUpdateService.StartSchedule();

            // activate plugins
            PluginManager.LoadedPlugins.ForEach(p => p.Initialize());

            // activate scripts
            ScriptingManagerImpl.Initialize();

            ReceiveManager.Initialize();
            TwitterConfigurationService.Initialize();
            BackstageModel.Initialize();
            RaiseSystemReady();
        }

        /// <summary>
        /// Show pre-execute option dialog (TaskDialog)
        /// </summary>
        /// <returns>when returning false, should abort execution</returns>
        private bool ShowPreExecuteDialog()
        {
            var resp = TaskDialog.Show(new TaskDialogOptions
            {
                Title = "Krileのメンテナンス",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "Krileが保持するデータを管理できます。",
                Content = "消去したデータはもとに戻せません。必要なデータは予めバックアップしてください。",
                CommandButtons = new[]
                {
                    /* 0 */ "このまま起動(&C)",
                    /* 1 */ "データベースを消去して起動(&D)",
                    /* 2 */ "すべての設定・データベースを消去して起動(&R)",
                    /* 3 */ "すべての設定・データベースを消去して終了(&E)",
                    /* 4 */ "最新版をクリーンインストール(&U)",
                    /* 5 */ "キャンセル(&X)"
                },
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "クリーンインストールを行うと、全ての設定・データベースが消去されます。"
            });
            if (!resp.CommandButtonResult.HasValue || resp.CommandButtonResult.Value == 5)
            {
                return false;
            }
            switch (resp.CommandButtonResult.Value)
            {
                case 1:
                    // remove database
                    if (File.Exists(DatabaseFilePath))
                    {
                        File.Delete(DatabaseFilePath);
                    }
                    break;
                case 2:
                case 3:
                case 4:
                    // remove all
                    if (ExecutionMode == ExecutionMode.Standalone)
                    {
                        // remove each
                        var files = new[]
                        {
                            DatabaseFilePath, DatabaseFilePath, HashtagTempFilePath, ListUserTempFilePath,
                            Path.Combine(ConfigurationDirectoryPath, ProfileFileName)
                        };
                        var dirs = new[]
                        {
                            KeyAssignProfilesDirectory
                        };
                        files.Where(File.Exists).ForEach(File.Delete);
                        dirs.Where(Directory.Exists).ForEach(d => Directory.Delete(d, true));
                    }
                    else
                    {
                        // remove whole directory
                        if (Directory.Exists(ConfigurationDirectoryPath))
                        {
                            Directory.Delete(ConfigurationDirectoryPath, true);
                        }
                    }
                    break;
            }
            if (resp.CommandButtonResult.Value == 4)
            {
                // force update
                var w = new AwaitDownloadingUpdateWindow();
                w.ShowDialog();
            }
            return resp.CommandButtonResult.Value < 3;
        }

        /// <summary>
        /// Check sqlite database
        /// </summary>
        /// <returns>when database is compatible, return true</returns>
        private bool CheckDatabase()
        {
            var ver = Database.ManagementCrud.DatabaseVersion;
            if (String.IsNullOrEmpty(ver))
            {
                Database.ManagementCrud.DatabaseVersion = DbVersion;
            }
            else if (ver != DbVersion)
            {
                // todo: update db
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finalize application
        /// </summary>
        /// <param name="shutdown">If true, application has shutdowned collectly.</param>
        void AppFinalize(bool shutdown)
        {
            if (shutdown)
            {
                Debug.WriteLine("App Normal Exit");
                RaiseApplicationExit();
            }
            Debug.WriteLine("App Finalize");
            RaiseApplicationFinalize();
            try
            {
                _appMutex.ReleaseMutex();
                _appMutex.Dispose();
            }
            catch (ObjectDisposedException)
            { }
        }

        /// <summary>
        /// Global unhandled exception handler method
        /// </summary>
        /// <param name="ex">thrown exception</param>
        private void HandleException(Exception ex)
        {
            try
            {
                var aex = ex as AggregateException;
                if (ex is SQLiteException || (aex != null && aex.InnerExceptions.Any(ie => ie is SQLiteException)) ||
                    ex.Message.Contains("database disk image is malformed"))
                {
                    // database error
                    Setting.DatabaseErrorOccured.Value = true;
                }

                if (ex.Message.Contains("8007007e") &&
                    ex.Message.Contains("CLSID {E5B8E079-EE6D-4E33-A438-C87F2E959254}"))
                {
                    Setting.DisableGeoLocationService.Value = false;
                }

                // TODO:ロギング処理など
                Debug.WriteLine("##### SYSTEM CRASH! #####");
                Debug.WriteLine(ex.ToString());

                // Build stack trace report file
                var builder = new StringBuilder();
                builder.AppendLine("Krile STARRYEYES #" + FormattedVersion + " - " + DateTime.Now.ToString());
                builder.AppendLine(Environment.OSVersion + " " + (Environment.Is64BitProcess ? "x64" : "x86"));
                builder.AppendLine("execution mode: " + ExecutionMode.ToString() + ", " +
                    "multicore JIT: " + IsMulticoreJitEnabled.ToString() + ", " +
                    "hardware rendering: " + IsHardwareRenderingEnabled.ToString());
                var uptime = DateTime.Now - _startupTime;
                builder.AppendLine("application uptime: " + ((int)uptime.TotalHours).ToString("00") +
                                   uptime.ToString("\\:mm\\:ss"));
                builder.AppendLine();
                builder.AppendLine("exception stack trace:");
                builder.AppendLine(ex.ToString());
#if DEBUG
                var tpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "StarryEyes_Dump_" + Path.GetRandomFileName() + ".txt");
                using (var sw = new StreamWriter(tpath))
                {
                    sw.WriteLine(builder.ToString());
                }
#else
                var tpath = Path.GetTempFileName() + ".crashlog";
                using (var sw = new StreamWriter(tpath))
                {
                    sw.WriteLine(builder.ToString());
                }
                var apppath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                var psi = new ProcessStartInfo
                {
                    Arguments = tpath,
                    UseShellExecute = true,
                    FileName = Path.Combine(apppath, App.FeedbackAppName)
                };
                try
                {
                    Process.Start(psi);
                }
                catch { }
#endif
            }
            finally
            {
                // アプリケーション ファイナライズ
                AppFinalize(false);
            }

            Environment.Exit(-1);
        }

        #region Definitions

        public const string AppShortName = "Krile";

        public const string AppFullName = "Krile STARRYEYES";

        internal static readonly string ConsumerKey = "oReH15myW0cPq7pHNvAMGw";

        internal static readonly string ConsumerSecret = "0BkDivB4cqdkKaTmXc8j2urtH9C4xApJBKZFvlb9dec";

        public static bool IsOperatingSystemSupported
        {
            get
            {
                return Environment.OSVersion.Version.Major == 6;
            }
        }

        [NotNull]
        public static string ExeFilePath
        {
            get
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
        }

        [NotNull]
        public static string ExeFileDir
        {
            get
            {
                return Path.GetDirectoryName(ExeFilePath) ?? ExeFilePath + "_";
            }
        }

        public static ExecutionMode ExecutionMode
        {
            get
            {
                switch (ConfigurationManager.AppSettings["ExecutionMode"].ToLower())
                {
                    case "standalone":
                    case "portable":
                        return ExecutionMode.Standalone;

                    case "roaming":
                        return ExecutionMode.Roaming;

                    default:
                        return ExecutionMode.Default;
                }
            }
        }

        public static string DatabaseDirectoryPath
        {
            get
            {
                try
                {
                    var path = ConfigurationManager.AppSettings["DatabaseDirectoryPath"];
                    if (String.IsNullOrEmpty(path))
                    {
                        // locate into configuration directory
                        return ConfigurationDirectoryPath;
                    }
                    // make sure to exist database directory
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show(new TaskDialogOptions
                    {
                        Title = "Krile スタートアップ エラー",
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "Krileの起動に失敗しました。",
                        Content = "データベース パスの指定が不正かもしれません。" + Environment.NewLine +
                                  "Krile.exe.configの設定を確認してください。",
                        ExpandedInfo = ex.ToString(),
                        CommonButtons = TaskDialogCommonButtons.Close,
                        FooterIcon = VistaTaskDialogIcon.Information,
                        FooterText = "改善しないときは、Krileの再インストールを試してください。"
                    });
                    throw;
                }
            }
        }

        public static bool IsMulticoreJitEnabled
        {
            get
            {
                if (ConfigurationManager.AppSettings["UseMulticoreJIT"].ToLower() == "none")
                    return false;
                return true;
            }
        }

        public static bool IsHardwareRenderingEnabled
        {
            get
            {
                if (ConfigurationManager.AppSettings["UseHardwareRendering"].ToLower() == "none")
                    return false;
                return true;
            }
        }

        [NotNull]
        public static string ConfigurationDirectoryPath
        {
            get
            {
                switch (ExecutionMode)
                {
                    case ExecutionMode.Standalone:
                        return ExeFileDir;
                    case ExecutionMode.Roaming:
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Krile");
                    default:
                        // setting hold in "Local"
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Krile");
                }
            }
        }

        [NotNull]
        public static string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(ConfigurationDirectoryPath, ConfigurationFileName);
            }
        }

        [NotNull]
        public static string DatabaseFilePath
        {
            get { return Path.Combine(DatabaseDirectoryPath, DatabaseFileName); }
        }

        public static string LocalUpdateStorePath
        {
            get { return Path.Combine(ConfigurationDirectoryPath, LocalUpdateStoreDirName); }
        }

        public static string HashtagTempFilePath
        {
            get { return Path.Combine(ConfigurationDirectoryPath, HashtagCacheFileName); }
        }

        public static string ListUserTempFilePath
        {
            get { return Path.Combine(ConfigurationDirectoryPath, ListCacheFileName); }
        }

        private static Version _version;

        [NotNull]
        public static Version Version
        {
            get
            {
                return _version ??
                       (_version = Assembly.GetEntryAssembly().GetName().Version);
            }
        }

        [NotNull]
        public static string FormattedVersion
        {
            get
            {
                var basestr = Version.ToString(3);
                if (Version.Revision > 0)
                {
                    return basestr + " Rev." + Version.Revision;
                }
                return basestr;
            }
        }

        public static bool IsUnstableVersion
        {
            get { return Version.Revision != 0; }
        }

        public static readonly uint LeastDesktopHeapSize = 12 * 1024;

        public static readonly string DatabaseFileName = "krile.db";

        public static readonly string KeyAssignProfilesDirectory = "assigns";

        public static readonly string ThemeProfilesDirectory = "themes";

        public static readonly string MediaDirectory = "media";

        public static readonly string PluginDirectory = "plugins";

        public static readonly string PluginSignatureFile = "auth.sig";

        public static readonly string ScriptDirectiory = "scripts";

        public static readonly string ConfigurationFileName = "krile.xml";

        public static readonly string HashtagCacheFileName = "tags.cache";

        public static readonly string ListCacheFileName = "lists.cache";

        public static readonly string ProfileFileName = "krile.profile";

        public static readonly string FeedbackAppName = "reporter.exe";

        public static readonly string UpdaterFileName = "kup.exe";

        public static readonly string DefaultStatusMessage = "完了";

        public static readonly string RemoteVersionXml = "http://krile.starwing.net/shared/update.xml";

        public static readonly string PublicKeyFile = "krile.pub";

        public static readonly string LocalUpdateStoreDirName = "update";

        public static readonly string MentionWavFile = "mention.wav";

        public static readonly string NewReceiveWavFile = "new.wav";

        public static readonly string DirectMessageWavFile = "directmessage.wav";

        public static readonly string EventWavFile = "event.wav";

        public static readonly string OfficialUrl = "http://krile.starwing.net/";

        public static readonly string QueryReferenceUrl = "https://github.com/karno/StarryEyes/wiki";

        public static readonly string AuthorizeHelpUrl = "https://github.com/karno/StarryEyes/wiki/Help_Authorize";

        public static readonly string ReleaseNoteUrl = "https://github.com/karno/StarryEyes/wiki/ReleaseNote";

        public static readonly string DonationUrl = "http://krile.starwing.net/donation.html";

        public static readonly string ContributorsUrl = "http://krile.starwing.net/shared/contrib.xml";

        public static readonly string LicenseUrl = "https://raw.github.com/karno/StarryEyes/master/LICENSE.TXT";

        #endregion

        #region Triggers

        /// <summary>
        /// Call on kernel systems are ready<para />
        /// (But UI is not prepared)
        /// </summary>
        public static event Action SystemReady;
        internal static void RaiseSystemReady()
        {
            Debug.WriteLine("# System ready.");
            var osr = SystemReady;
            SystemReady = null;
            if (osr != null)
                osr();
        }

        /// <summary>
        /// Call on user interfaces are ready
        /// </summary>
        public static event Action UserInterfaceReady;
        internal static void RaiseUserInterfaceReady()
        {
            // this method called by background thread.
            Debug.WriteLine("# UI ready.");
            var usr = UserInterfaceReady;
            UserInterfaceReady = null;
            if (usr != null)
                usr();
        }

        /// <summary>
        /// Call on aplication is exit from user action<para />
        /// (On crash app, this handler won't call!)
        /// </summary>
        public static event Action ApplicationExit;
        internal static void RaiseApplicationExit()
        {
            Debug.WriteLine("# App exit.");
            var apx = ApplicationExit;
            ApplicationExit = null;
            if (apx != null)
                apx();
        }

        /// <summary>
        /// Call on application is exit from user action or crashed
        /// </summary>
        public static event Action ApplicationFinalize;
        internal static void RaiseApplicationFinalize()
        {
            Debug.WriteLine("# App finalize.");
            var apf = ApplicationFinalize;
            ApplicationFinalize = null;
            if (apf != null)
                apf();
        }

        #endregion

        public static readonly DateTime StartupDateTime = DateTime.Now;
    }

    public enum ExecutionMode
    {
        /// <summary>
        /// Use Local folder for storing setting file
        /// </summary>
        Default,
        /// <summary>
        /// Use Roaming folder for storing setting file
        /// </summary>
        Roaming,
        /// <summary>
        /// Use application local folder for storing setting file
        /// </summary>
        Standalone,
    }
}
