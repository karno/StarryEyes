using Livet;
using StarryEyes.Breezy.Api;
using StarryEyes.Models.Plugins;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Windows;
using System.Text;
using TaskDialogInterop;

namespace StarryEyes
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static Mutex appMutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // enable multi-core JIT.
            // see reference: http://msdn.microsoft.com/en-us/library/system.runtime.profileoptimization.aspx
            if (IsMulticoreJITEnabled)
            {
                ProfileOptimization.SetProfileRoot(ConfigurationDirectoryPath);
                ProfileOptimization.StartProfile(ProfileFileName);
            }

            // initialize dispatcher helper
            DispatcherHelper.UIDispatcher = Dispatcher;

            // set rendering mode
            if (!IsHardwareRenderingEnabled)
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }

            // Check run duplication
            string mutexStr = null;
            switch (ExecutionMode)
            {
                case ExecutionMode.Default:
                case ExecutionMode.Roaming:
                    mutexStr = ExecutionMode.ToString();
                    break;
                case ExecutionMode.Standalone:
                    mutexStr = "Standalone_" + ExeFilePath;
                    break;
            }
            appMutex = new Mutex(true, "Krile_StarryEyes_" + mutexStr);
            if (appMutex.WaitOne(0, false) == false)
            {
                MessageBox.Show("Krileは既に起動しています。");
                Environment.Exit(0);
                return;
            }

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            }
#else
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            Application.Current.Exit += (_, __) => AppFinalize(true);

            // Initialize service points
            ServicePointManager.Expect100Continue = false; // disable expect 100 continue for User Streams connection.
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue; // Limit Break!

            // Load plugins
            PluginManager.Load();

            // Load settings
            if (!Setting.LoadSettings())
            {
                Application.Current.Shutdown();
                return; // fail
            }

            // Set CK/CS for accessing twitter.
            ApiEndpoint.DefaultConsumerKey = Setting.GlobalConsumerKey.Value ?? ConsumerKey;
            ApiEndpoint.DefaultConsumerSecret = Setting.GlobalConsumerSecret.Value ?? ConsumerSecret;

            // Initialize core systems
            try
            {
                StatusStore.Initialize();
                UserStore.Initialize();
            }
            catch (Exception ex)
            {
                var option = new TaskDialogOptions()
                {
                    Title = "Krile データストア初期化エラー",
                    MainInstruction = "Krileの起動中にエラーが発生しました。",
                    Content = "データストアが破損しています。" + Environment.NewLine +
                    "データストアを初期化するか、またはKrileを終了してバックアップを取ることができます。",
                    ExpandedInfo = ex.ToString(),
                    CommandButtons = new[] { "データストアを初期化して再起動", "Krileを終了" }
                };
                var result = TaskDialog.Show(option);
                if (!result.CommandButtonResult.HasValue ||
                    result.CommandButtonResult.Value == 1)
                {
                    // shutdown
                    Application.Current.Shutdown();
                    return;
                }
                else
                {
                    // clear data
                    ClearStoreData();
                    Nightmare.Windows.Application.Restart();
                    Application.Current.Shutdown();
                    return;
                }
            }
            AccountsStore.Initialize();
            StatisticsService.Initialize();

            // Activate plugins
            PluginManager.LoadedPlugins.ForEach(p => p.Initialize());

            // Activate scripts
            ScriptingManager.ExecuteScripts();

            RaiseSystemReady();
        }

        /// <summary>
        /// アプリケーションのファイナライズ
        /// </summary>
        /// <param name="shutdown">アプリケーションが正しく終了している状態か</param>
        void AppFinalize(bool shutdown)
        {
            if (shutdown)
            {
                System.Diagnostics.Debug.WriteLine("App Normal Exit");
                // 正規の終了
                RaiseApplicationExit();
            }
            System.Diagnostics.Debug.WriteLine("App Finalize");
            RaiseApplicationFinalize();
            appMutex.ReleaseMutex();
        }

        /// <summary>
        /// Error handler
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                // TODO:ロギング処理など
                System.Diagnostics.Debug.WriteLine("##### SYSTEM CRASH! #####");
                System.Diagnostics.Debug.WriteLine(e.ExceptionObject.ToString());

                // Build stack trace report file
                var builder = new StringBuilder();
                builder.AppendLine("Krile STARRYEYES #" + App.FormattedVersion);
                builder.AppendLine(Environment.OSVersion.ToString() + " " + (Environment.Is64BitProcess ? "x64" : "x86"));
                builder.AppendLine("execution mode: " + App.ExecutionMode.ToString() + " " +
                    "multicore JIT: " + App.IsMulticoreJITEnabled.ToString() + " " +
                    "hardware rendering: " + App.IsHardwareRenderingEnabled.ToString());
                builder.AppendLine();
                builder.AppendLine("thrown:");
                builder.AppendLine(e.ExceptionObject.ToString());
#if DEBUG
                var tpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "StarryEyes_Dump_" + Path.GetRandomFileName() + ".txt");
                using (var sw = new StreamWriter(tpath))
                {
                    sw.WriteLine(builder.ToString());
                }
#else
                var tpath = Path.GetTempFileName();
                using (var sw = new StreamWriter(tpath))
                {
                    sw.WriteLine(builder.ToString());
                }
                var apppath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                System.Diagnostics.Process.Start(Path.Combine(apppath, App.FeedbackAppName), tpath);
#endif
            }
            finally
            {
                // アプリケーション ファイナライズ
                this.AppFinalize(false);
            }


            Environment.Exit(-1);
        }

        /// <summary>
        /// Clear all data stored in data store.
        /// </summary>
        internal void ClearStoreData()
        {
            Directory.Delete(DataStorePath, true);
        }

        #region Definitions

        internal static string ConsumerKey = "HzbATXmr3JpNXRPDNtkXww";

        internal static string ConsumerSecret = "BfBH1h68S45X4kAlVdJAoopbEIEyF43kaRQe1vC2po";

        public static bool IsOperatingSystemSupported
        {
            get
            {
                return Environment.OSVersion.Version.Major == 6;
            }
        }

        public static string ExeFilePath
        {
            get
            {
                return Process.GetCurrentProcess().MainModule.FileName;
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

        public static bool IsMulticoreJITEnabled
        {
            get
            {
                if (ConfigurationManager.AppSettings["UseMulticoreJIT"].ToLower() == "none")
                    return false;
                else
                    return true;
            }
        }

        public static bool IsHardwareRenderingEnabled
        {
            get
            {
                if (ConfigurationManager.AppSettings["UseHardwareRendering"].ToLower() == "none")
                    return false;
                else
                    return true;
            }
        }

        public static string ConfigurationDirectoryPath
        {
            get
            {
                switch (ExecutionMode)
                {
                    case ExecutionMode.Standalone:
                        return Path.GetDirectoryName(ExeFilePath);
                    case ExecutionMode.Roaming:
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Krile");
                    case ExecutionMode.Default:
                    default:
                        // setting hold in "Local"
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Krile");
                }
            }
        }

        public static string ConfigurationFilePath
        {
            get
            {
                return Path.Combine(ConfigurationDirectoryPath, ConfigurationFileName);
            }
        }

        public static string DataStorePath
        {
            get
            {
                return Path.Combine(ConfigurationDirectoryPath, DataStoreDirectory);
            }
        }

        private static FileVersionInfo _version;
        public static FileVersionInfo Version
        {
            get
            {
                return _version ??
                    (_version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location));
            }
        }

        public static string FormattedVersion
        {
            get
            {
                return Version.FileMajorPart + "." +
                    Version.FileMinorPart + "." +
                    Version.FilePrivatePart + FileKind(Version.FileBuildPart);
            }
        }

        public static bool IsNightlyVersion
        {
            get
            {
                return Version.FilePrivatePart >= 1;
            }
        }

        private static string FileKind(int value)
        {
            switch (value)
            {
                case 0:
                    return String.Empty;
                case 1:
                    return " BETA"; // release test version
                case 2:
                    return " UNSTABLE"; // periodic test version
                case 3:
                    return " NIGHTMARE"; // under construction version
                default:
                    return " #" + value;
            }
        }

        public static double NumericVersion
        {
            get
            {
                var lvstr = (Version.FileMajorPart * 1000 + Version.FileMinorPart).ToString() + "." +
                    Version.FilePrivatePart.ToString();
                return Double.Parse(lvstr);
            }
        }

        public static readonly string DataStoreDirectory = "store";

        public static readonly string KeyAssignDirectory = "assigns";

        public static readonly string MediaDirectory = "media";

        public static readonly string PluginDirectory = "plugins";

        public static readonly string PluginDescriptionFile = "package.xml";

        public static readonly string PluginPublicKeyFile = "pubkey.pub";

        public static readonly string PluginSignatureFile = "auth.sig";

        public static readonly string ScriptDirectiory = "scripts";

        public static readonly string ConfigurationFileName = "krile.xml";

        public static readonly string ProfileFileName = "krile.profile";

        public static readonly string FeedbackAppName = "reporter.exe";

        public static readonly string UpdateFileName = "kup.exe";

        public static readonly string DefaultStatusMessage = "完了";

        public static readonly string RemoteVersionXml = "http://update.starwing.net/starryeyes/update.xml";

        public static readonly string PublicKeyFile = "kup.pub";

        public static readonly string MentionWavFile = "mention.wav";

        public static readonly string NewReceiveWavFile = "new.wav";

        public static readonly string DirectMessageWavFile = "directmessage.wav";

        public static readonly string EventWavFile = "event.wav";

        public static readonly string ReleaseNoteUrl = "http://krile.starwing.net/updates.html";

        public static readonly string KampaUrl = "http://krile.starwing.net/kampa.html";

        #endregion

        #region Triggers

        /// <summary>
        /// Call on kernel systems are ready<para />
        /// (But UI is not prepared)
        /// </summary>
        public static event Action OnSystemReady;
        internal static void RaiseSystemReady()
        {
            var osr = OnSystemReady;
            OnSystemReady = null;
            if (osr != null)
                osr();
        }

        /// <summary>
        /// Call on user interfaces are ready
        /// </summary>
        public static event Action OnUserInterfaceReady;
        internal static void RaiseUserInterfaceReady()
        {
            var usr = OnUserInterfaceReady;
            OnUserInterfaceReady = null;
            if (usr != null)
                usr();
        }

        /// <summary>
        /// Call on aplication is exit from user action<para />
        /// (On crash app, this handler won't call!)
        /// </summary>
        public static event Action OnApplicationExit;
        internal static void RaiseApplicationExit()
        {
            var apx = OnApplicationExit;
            OnApplicationExit = null;
            if (apx != null)
                apx();
        }

        /// <summary>
        /// Call on application is exit from user action or crashed
        /// </summary>
        public static event Action OnApplicationFinalize;
        internal static void RaiseApplicationFinalize()
        {
            var apf = OnApplicationFinalize;
            OnApplicationFinalize = null;
            if (apf != null)
                apf();
        }

        #endregion
    }

    public enum ExecutionMode
    {
        Default,
        Roaming,
        Standalone,
    }
}
