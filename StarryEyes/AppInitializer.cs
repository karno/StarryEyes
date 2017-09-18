using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Livet;
using StarryEyes.Casket;
using StarryEyes.Globalization;
using StarryEyes.Models;
using StarryEyes.Models.Plugins;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Subsystems.Notifications.UI;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Dialogs;
using Application = System.Windows.Application;

namespace StarryEyes
{
    internal static class AppInitializer
    {
        private static Mutex _appMutex;

        #region pre-initialize application

        public static void PreInitialize(StartupEventArgs e)
        {
            var isMaintenanceMode = e.Args.Select(a => a.ToLower()).Contains("-maintenance");

            PrepareConfigurationDirectory();
            SetSystemParameters(App.IsMulticoreJitEnabled && !isMaintenanceMode,
                App.IsHardwareRenderingEnabled);

            if (!isMaintenanceMode && !CheckInitializeMutex())
            {
                FailMessages.LaunchDuplicated();
                Environment.Exit(-1);
            }
        }

        private static void PrepareConfigurationDirectory()
        {
            // create data-store directory
            try
            {
                Directory.CreateDirectory(App.ConfigurationDirectoryPath);
            }
            catch (Exception ex)
            {
                FailMessages.InitConfDirFailed(ex);
                Environment.Exit(-1);
            }
        }

        private static void SetSystemParameters(bool useProfiling, bool useHardwareRendering)
        {
            // enable multi-core JIT.
            // see reference: http://msdn.microsoft.com/en-us/library/system.runtime.profileoptimization.aspx
            if (useProfiling)
            {
                ProfileOptimization.SetProfileRoot(App.ConfigurationDirectoryPath);
                ProfileOptimization.StartProfile(App.ProfileFileName);
            }

            // initialize dispatcher helper
            DispatcherHelper.UIDispatcher = Application.Current.Dispatcher;

            // set rendering mode
            if (!useHardwareRendering)
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
        }

        #endregion pre-initialize application

        #region initialize application

        public static void Initialize(StartupEventArgs e)
        {
            var isMaintenanceMode = e.Args.Select(a => a.ToLower()).Contains("-maintenance");
            var postUpdate = e.Args.Select(a => a.ToLower()).Contains("-postupdate");

            // determine mode and rescue/maintenance self
            if (!CheckRescueMaintenance(isMaintenanceMode))
            {
                Environment.Exit(-1);
            }

            if (postUpdate || AutoUpdateService.IsPostUpdateFileExisted())
            {
                // remove update binary
                AutoUpdateService.PostUpdate();
            }
            else if (AutoUpdateService.IsUpdateBinaryExisted())
            {
                // execute auto-update
                AutoUpdateService.StartUpdate(App.Version);
                Environment.Exit(0);
            }

            try
            {
                // create lock file
                using (File.Create(App.LockFilePath))
                {
                    // do nothing
                }
            }
            catch (Exception ex)
            {
                FailMessages.InitLockFailed(ex);
                Environment.Exit(-1);
            }

            // initialze web parameters
            InitializeWebConnectionParameters();

            // load subsystems, load settings
            InitializeSubsystemsBeforeSettingsLoaded();
            if (!Setting.LoadSettings())
            {
                // failed loading settings
                Environment.Exit(-1);
            }

            // prepare user anonymous id

            InitializeSubsystemsAfterSettingsLoaded();
        }

        /// <summary>
        /// Show rescue dialog or maintenance dialog
        /// </summary>
        /// <param name="isMaintenanceMode">flag of maintenance is explicitly designated</param>
        /// <returns>if false, aplication should be halted.</returns>
        private static bool CheckRescueMaintenance(bool isMaintenanceMode)
        {
            return isMaintenanceMode
                ? ShowMaintenanceDialog()
                : !File.Exists(App.LockFilePath) || ShowRescueDialog();
        }

        /// <summary>
        /// Show rescue option dialog (TaskDialog)
        /// </summary>
        /// <returns>when returning false, should abort execution</returns>
        private static bool ShowRescueDialog()
        {
#if DEBUG
            return true;
#else
            var resp = ConfirmationMassages.ConfirmRescue();
            if (!resp.CommandButtonResult.HasValue || resp.CommandButtonResult.Value == 2)
            {
                return false;
            }
            if (resp.CommandButtonResult.Value == 0)
            {
                return true;
            }
            return ShowMaintenanceDialog();
#endif
        }

        /// <summary>
        /// Show maintenance option dialog (TaskDialog)
        /// </summary>
        /// <returns>when returning false, should abort execution</returns>
        private static bool ShowMaintenanceDialog()
        {
            var resp = ConfirmationMassages.ConfirmMaintenance();
            if (!resp.CommandButtonResult.HasValue || resp.CommandButtonResult.Value == 5)
            {
                return false;
            }
            try
            {
                switch (resp.CommandButtonResult.Value)
                {
                    case 1:
                        // optimize database
                        ScheduleDatabaseOptimization();
                        break;
                    case 2:
                        // remove database
                        if (File.Exists(App.DatabaseFilePath))
                        {
                            File.Delete(App.DatabaseFilePath);
                        }
                        break;
                    case 3:
                    case 4:
                    case 5:
                        // remove all
                        if (App.ExecutionMode == ExecutionMode.Standalone)
                        {
                            // remove each
                            var files = new[]
                            {
                                App.DatabaseFilePath, App.DatabaseFilePath,
                                App.HashtagTempFilePath, App.ListUserTempFilePath,
                                Path.Combine(App.ConfigurationDirectoryPath, App.ProfileFileName)
                            };
                            var dirs = new[]
                            {
                                App.KeyAssignProfilesDirectory
                            };
                            files.Where(File.Exists).ForEach(File.Delete);
                            dirs.Where(Directory.Exists).ForEach(d => Directory.Delete(d, true));
                        }
                        else
                        {
                            // remove whole directory
                            if (Directory.Exists(App.ConfigurationDirectoryPath))
                            {
                                Directory.Delete(App.ConfigurationDirectoryPath, true);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                FailMessages.GeneralProcessFailed(ex);
            }
            if (resp.CommandButtonResult.Value == 5)
            {
                // force update
                var w = new AwaitDownloadingUpdateWindow();
                w.ShowDialog();
            }
            return resp.CommandButtonResult.Value < 4;
        }

        /// <summary>
        /// Initialize service-point manager
        /// </summary>
        private static void InitializeWebConnectionParameters()
        {
            // initialize service points
            ServicePointManager.Expect100Continue = false; // disable expect 100 continue for User Streams connection.
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue; // Limit Break!

            // declare security protocol explicitly
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            // if operating system supports more secure protocol, use it.
            var osv = Environment.OSVersion.Version;
            if (osv.Major == 6 && osv.Minor >= 1)
            {
                // TLS 1.2 is supported on Windows 7(NT 6.1) or later.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }

        private static void InitializeSubsystemsBeforeSettingsLoaded()
        {
            // initialize anomaly twitter library core system
            Anomaly.Core.Initialize();

            // initialize special image handlers
            SpecialImageResolvers.Initialize();

            // load plugins
            PluginManager.Load(Path.Combine(App.ExeFileDir, App.PluginDirectory));
        }

        private static void InitializeSubsystemsAfterSettingsLoaded()
        {
            // Logger subsystem initialize
            BehaviorLogger.Initialize();

            // set parameters for accessing twitter.
            Networking.Initialize();

            // load key assigns
            KeyAssignManager.Initialize();

            // load cache manager
            CacheStore.Initialize();
        }

        #endregion initialize application

        #region Tweet cleanup

        public static void CleanupTweetsIfRequired()
        {
            try
            {
                var threshold = Setting.AutoCleanupThreshold.Value;
                if (!Setting.AutoCleanupTweets.Value || threshold <= 0) return;
                var cleanup = Database.StatusCrud.GetCountAsync();
                if (cleanup.Wait(TimeSpan.FromSeconds(0.1)) && cleanup.Result < threshold * 2)
                {
                    return;
                }
            }
            catch (SqliteCrudException)
            {
            }
            ShowWorkingDialog("cleaning tweets...", CleanupStatus);
        }

        private static async Task CleanupStatus(Action<string> state)
        {
            var threshold = Setting.AutoCleanupThreshold.Value;
            try
            {
                // check status count
                var cleanup = Database.StatusCrud.GetCountAsync();
                if (cleanup.Wait(TimeSpan.FromSeconds(0.1)) && cleanup.Result < threshold * 2)
                {
                    return;
                }

                // delete old status
                await Database.CleanupOldStatusesAsync(threshold,
                        t => state("cleaning tweets (" + t.Item1 + "/" + t.Item2 + ")..."));

                // require optimizing
                _isDatabaseOptimizationRequired = true;
            }
            catch (SqliteCrudException cex)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon = VistaTaskDialogIcon.Error,
                    Title = AppInitResources.MsgDbCleanupFailedTitle,
                    MainInstruction = AppInitResources.MsgDbCleanupFailedInst,
                    Content = cex.Message,
                    ExpandedInfo = cex.ToString()
                });
            }
        }

        #endregion Tweet cleanup

        #region Database optimization

        private static bool _isDatabaseOptimizationRequired;

        private static void ScheduleDatabaseOptimization()
        {
            _isDatabaseOptimizationRequired = true;
        }

        public static void OptimizeDatabaseIfRequired()
        {
            if (!_isDatabaseOptimizationRequired) return;
            ShowWorkingDialog("optimizing database...", OptimizeDatabase);
        }

        private static async Task OptimizeDatabase()
        {
            try
            {
                await Database.VacuumTables();
            }
            catch (SqliteCrudException cex)
            {
                TaskDialog.Show(new TaskDialogOptions
                {
                    MainIcon = VistaTaskDialogIcon.Error,
                    Title = AppInitResources.MsgDbOptimizationFailedTitle,
                    MainInstruction = AppInitResources.MsgDbOptimizationFailedInst,
                    Content = cex.Message,
                    ExpandedInfo = cex.ToString()
                });
            }
        }

        #endregion Database optimization

        #region post-initialize application

        public static void PostInitialize()
        {
            // initialize subsystems
            StatisticsService.Initialize();
            PostLimitPredictionService.Initialize();
            MuteBlockManager.Initialize();
            StatusBroadcaster.Initialize();
            StatusInbox.Initialize();
            AutoUpdateService.StartSchedule();
            NotificationService.Initialize();
            UINotificationProxy.Initialize();

            // activate plugins
            PluginManager.LoadedPlugins.ForEach(p => p.Initialize());

            // activate scripts
            ScriptingManagerImpl.Initialize();

            // other core systems initialize
            ReceiveManager.Initialize();
            TwitterConfigurationService.Initialize();
            BackstageModel.Initialize();
        }

        #endregion post-initialize application

        #region Mutex control

        private static bool CheckInitializeMutex()
        {
            // if Krile started as maintenance mode, skip this check.
            string mutexStr = null;
            switch (App.ExecutionMode)
            {
                case ExecutionMode.Default:
                case ExecutionMode.Roaming:
                    mutexStr = App.ExecutionMode.ToString();
                    break;
                case ExecutionMode.Standalone:
                    mutexStr = "Standalone_" + App.ExeFilePath.Replace('\\', '*');
                    break;
            }

            // create mutex
            _appMutex = new Mutex(true, "Krile_StarryEyes_" + mutexStr);

            // check mutex
            return _appMutex.WaitOne(0, false);
        }

        public static void ReleaseMutex()
        {
            try
            {
                if (_appMutex == null) return;
                _appMutex.ReleaseMutex();
                _appMutex.Dispose();
                _appMutex = null;
            }
            catch (ObjectDisposedException) { }
        }

        #endregion Mutex control

        #region Confirmation messages

        public static class ConfirmationMassages
        {
            public static TaskDialogResult ConfirmRescue()
            {
                return ShowMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgRecoveryTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = AppInitResources.MsgRecoveryInst,
                    Content = AppInitResources.MsgRecoveryContent,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = AppInitResources.MsgRecoveryFooter,
                    CommandButtons = new[]
                    {
                        /* 0 */ AppInitResources.MsgRecoveryCmdContinue,
                        /* 1 */ AppInitResources.MsgRecoveryCmdMaintenance,
                        /* 2 */ AppInitResources.MsgRecoveryCmdExit
                    }
                });
            }

            public static TaskDialogResult ConfirmMaintenance()
            {
                return ShowMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgMaintenanceTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = AppInitResources.MsgMaintenanceInst,
                    Content = AppInitResources.MsgMaintenanceContent,
                    CommandButtons = new[]
                    {
                        /* 0 */ AppInitResources.MsgMaintenanceCmdContinue,
                        /* 1 */ AppInitResources.MsgMaintenanceCmdOptimize,
                        /* 2 */ AppInitResources.MsgMaintenanceCmdDeleteDb,
                        /* 3 */ AppInitResources.MsgMaintenanceCmdDeleteAll,
                        /* 4 */ AppInitResources.MsgMaintenanceCmdDeleteAllAndExit,
                        /* 5 */ AppInitResources.MsgMaintenanceCmdCleanInstall,
                        /* 6 */ AppInitResources.MsgMaintenanceCmdExit
                    }
                });
            }

        }
        #endregion Confirmation messages

        #region Fail messages

        /// <summary>
        /// Failed messages
        /// </summary>
        private static class FailMessages
        {

            public static void LaunchDuplicated()
            {
                ShowMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgFailTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = AppInitResources.MsgFailLaunchDuplicatedInst,
                    Content = AppInitResources.MsgFailLaunchDuplicatedContent,
                    ExpandedInfo = AppInitResources.MsgFailLaunchDuplicatedExInfo,
                    CommonButtons = TaskDialogCommonButtons.Close
                });
            }

            public static void InitConfDirFailed(Exception ex)
            {
                ShowMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgFailTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = AppInitResources.MsgFailStartInst,
                    Content = AppInitResources.MsgFailInitConfDirContent,
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = AppInitResources.MsgFailInitConfDirFooterText,
                });
            }

            public static void InitLockFailed(Exception ex)
            {
                ShowMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgFailTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = AppInitResources.MsgFailStartInst,
                    Content = AppInitResources.MsgFailInitLockContent,
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = AppInitResources.MsgFailInitLockFooter
                });
            }

            public static void GeneralProcessFailed(Exception ex)
            {
                ShowMessage(new TaskDialogOptions
                {
                    Title = App.AppFullName,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = AppInitResources.MsgFailGeneralProcessInst,
                    Content = AppInitResources.MsgFailGeneralProcessContent,
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                });
            }
        }

        #endregion Fail messages

        private static TaskDialogResult ShowMessage(TaskDialogOptions option)
        {
            // Suppress auto shutdown
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                return TaskDialog.Show(option);
            }
            finally
            {
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
        }

        private static void ShowWorkingDialog(string description, Func<Task> executeTask)
        {
            // change application shutdown mode for preventing 
            // auto-exit when optimization is completed.
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                // run database optimization
                var optDlg = new WorkingWindow(
                    description, executeTask);
                optDlg.ShowDialog();
            }
            finally
            {
                // restore shutdown mode
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
        }

        private static void ShowWorkingDialog(string description, Func<Action<string>, Task> executeTask)
        {
            // change application shutdown mode for preventing 
            // auto-exit when optimization is completed.
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                // run database optimization
                var optDlg = new WorkingWindow(
                    description, executeTask);
                optDlg.ShowDialog();
            }
            finally
            {
                // restore shutdown mode
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
        }
    }
}