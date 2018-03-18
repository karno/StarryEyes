using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging;
using StarryEyes.Globalization;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Properties;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.ViewModels.WindowParts.Flips;
using StarryEyes.ViewModels.WindowParts.Inputting;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        #region Included viewmodels

        public BackstageViewModel BackstageViewModel { get; }

        public InputViewModel InputViewModel { get; }

        public MainAreaViewModel MainAreaViewModel { get; }

        public AccountSelectionFlipViewModel InputAreaAccountSelectionFlipViewModel => InputViewModel
            .AccountSelectorViewModel.AccountSelectionFlip;

        public AccountSelectionFlipViewModel GlobalAccountSelectionFlipViewModel { get; }

        public SettingFlipViewModel SettingFlipViewModel { get; }

        public TabConfigurationFlipViewModel TabConfigurationFlipViewModel { get; }

        public SearchFlipViewModel SearchFlipViewModel { get; }

        #endregion Included viewmodels

        #region Properties

        public string WindowTitle => !App.IsUnstableVersion
            ? App.AppFullName
            : App.AppFullName + " " + App.FormattedVersion;

        public bool ShowWindowCommands
        {
            get => ShowSearchBox;
            set
            {
                ShowSearchBox = value;
                RaisePropertyChanged(() => ShowWindowCommands);
            }
        }

        public bool ShowSettingLabel => ShowSearchBox && SearchFlipViewModel.IsVisible;

        public bool ShowSearchBox { get; private set; } = true;

        #endregion Properties

        public MainWindowViewModel()
        {
            CompositeDisposable.Add(BackstageViewModel = new BackstageViewModel());
            CompositeDisposable.Add(InputViewModel = new InputViewModel());
            CompositeDisposable.Add(MainAreaViewModel = new MainAreaViewModel());
            CompositeDisposable.Add(GlobalAccountSelectionFlipViewModel = new AccountSelectionFlipViewModel());
            CompositeDisposable.Add(SettingFlipViewModel = new SettingFlipViewModel(this));
            CompositeDisposable.Add(TabConfigurationFlipViewModel = new TabConfigurationFlipViewModel());
            CompositeDisposable.Add(SearchFlipViewModel = new SearchFlipViewModel());
            CompositeDisposable.Add(Observable
                .FromEvent<FocusRequest>(
                    h => MainWindowModel.FocusRequested += h,
                    h => MainWindowModel.FocusRequested -= h)
                .Subscribe(SetFocus));
            CompositeDisposable.Add(Observable
                .FromEvent<bool>(
                    h => MainWindowModel.BackstageTransitionRequested += h,
                    h => MainWindowModel.BackstageTransitionRequested -= h)
                .Subscribe(TransitionBackstage));
            CompositeDisposable.Add(Setting.BackgroundImagePath.ListenValueChanged(
                _ =>
                {
                    RaisePropertyChanged(() => BackgroundImageUri);
                    RaisePropertyChanged(() => BackgroundImage);
                }));
            CompositeDisposable.Add(Setting.BackgroundImageTransparency.ListenValueChanged(
                _ => RaisePropertyChanged(() => BackgroundImageOpacity)));
            BackstageViewModel.Initialize();
        }

        private void SetFocus(FocusRequest req)
        {
            switch (req)
            {
                case FocusRequest.Search:
                    SearchFlipViewModel.FocusToSearchBox();
                    break;
                case FocusRequest.Timeline:
                    SearchFlipViewModel.Close();
                    var ctab = TabManager.CurrentFocusTab;
                    ctab?.RequestFocus();
                    break;
                case FocusRequest.Input:
                    SearchFlipViewModel.Close();
                    InputViewModel.OpenInput();
                    InputViewModel.FocusToTextBox();
                    break;
            }
        }

        private int _visibleCount;

        [UsedImplicitly]
        public void Initialize()
        {
            #region bind events

            CompositeDisposable.Add(
                Observable.FromEvent<bool>(
                              h => MainWindowModel.WindowCommandsDisplayChanged += h,
                              h => MainWindowModel.WindowCommandsDisplayChanged -= h)
                          .Subscribe(visible =>
                          {
                              var offset = visible
                                  ? Interlocked.Increment(ref _visibleCount)
                                  : Interlocked.Decrement(ref _visibleCount);
                              ShowWindowCommands = offset >= 0;
                          }));
            CompositeDisposable.Add(
                Observable.FromEvent<TaskDialogOptions>(
                              h => MainWindowModel.TaskDialogRequested += h,
                              h => MainWindowModel.TaskDialogRequested -= h)
                          .Subscribe(options => Messenger.RaiseSafe(() =>
                              new TaskDialogMessage(options))));
            CompositeDisposable.Add(
                Observable.FromEvent(
                              h => MainWindowModel.StateStringChanged += h,
                              h => MainWindowModel.StateStringChanged -= h)
                          .Subscribe(_ => RaisePropertyChanged(() => StateString)));
            CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(0.5))
                          .Subscribe(_ => UpdateStatistics()));
            CompositeDisposable.Add(
                Observable.FromEvent<AccountSelectDescription>(
                              h => MainWindowModel.AccountSelectActionRequested += h,
                              h => MainWindowModel.AccountSelectActionRequested -= h)
                          .Subscribe(
                              desc =>
                              {
                                  // ensure close before opening.
                                  GlobalAccountSelectionFlipViewModel.Close();

                                  GlobalAccountSelectionFlipViewModel.SelectedAccounts =
                                      desc.SelectionAccounts;
                                  GlobalAccountSelectionFlipViewModel.SelectionReason = "";
                                  switch (desc.AccountSelectionAction)
                                  {
                                      case AccountSelectionAction.Favorite:
                                          GlobalAccountSelectionFlipViewModel.SelectionReason =
                                              "favorite";
                                          break;
                                      case AccountSelectionAction.Retweet:
                                          GlobalAccountSelectionFlipViewModel.SelectionReason =
                                              "retweet";
                                          break;
                                  }
                                  IDisposable disposable = null;
                                  disposable = Observable.FromEvent(
                                                             h => GlobalAccountSelectionFlipViewModel.Closed += h,
                                                             h => GlobalAccountSelectionFlipViewModel.Closed -= h)
                                                         .Subscribe(_ =>
                                                         {
                                                             if (disposable == null) return;
                                                             disposable.Dispose();
                                                             disposable = null;
                                                             desc.Callback(
                                                                 GlobalAccountSelectionFlipViewModel
                                                                     .SelectedAccounts);
                                                         });
                                  GlobalAccountSelectionFlipViewModel.Open();
                              }));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => ThemeManager.ThemeChanged += h,
                    h => ThemeManager.ThemeChanged -= h
                ).Subscribe(_ => Messenger?.RaiseAsync(new InteractionMessage("InvalidateTheme"))));

            #endregion bind events

            #region special navigations

            // check first boot
            if (Setting.IsFirstGenerated)
            {
                var kovm = new KeyOverrideViewModel();
                Messenger.RaiseSafeSync(() => new TransitionMessage(
                    typeof(KeyOverrideWindow),
                    kovm, TransitionMode.Modal, null));
            }

            // register new account if accounts haven't been authorized yet
            if (!Setting.Accounts.Collection.Any())
            {
                var auth = new AuthorizationViewModel();
                auth.AuthorizeObservable
                    .Subscribe(Setting.Accounts.Collection.Add);
                Messenger.RaiseSafeSync(() => new TransitionMessage(
                    typeof(AuthorizationWindow),
                    auth, TransitionMode.Modal, null));
            }

            #endregion special navigations

            TabManager.Load();
            TabManager.Save();

            if (TabManager.Columns.Count == 1 && TabManager.Columns[0].Tabs.Count == 0)
            {
                // lost tab info
                ReInitTabs();
            }

            // check cleanup parameter
            if (Setting.AutoCleanupTweets.Value &&
                Setting.AutoCleanupThreshold.Value < 0)
            {
                var msg = Messenger.GetResponseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = AppInitResources.MsgCleanupConfigTitle,
                        MainIcon = VistaTaskDialogIcon.Information,
                        MainInstruction = AppInitResources.MsgCleanupConfigInst,
                        Content = AppInitResources.MsgCleanupConfigContent,
                        ExpandedInfo = AppInitResources.MsgCleanupConfigExInfo,
                        CommonButtons = TaskDialogCommonButtons.YesNo
                    }));
                Setting.AutoCleanupTweets.Value = msg.Response.Result == TaskDialogSimpleResult.Yes;
                Setting.AutoCleanupThreshold.Value = 100000;
            }

            // check execution properties
            if (Setting.ShowStartupConfigurationWarning.Value)
            {
                if (App.ExecutionMode == ExecutionMode.Standalone)
                {
                    var msg = Messenger.GetResponseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = AppInitResources.MsgExecModeWarningTitle,
                            MainIcon = VistaTaskDialogIcon.Warning,
                            MainInstruction = AppInitResources.MsgExecModeWarningInst,
                            Content = AppInitResources.MsgExecModeWarningContent,
                            FooterIcon = VistaTaskDialogIcon.Error,
                            FooterText = AppInitResources.MsgExecModeWarningFooter,
                            CommonButtons = TaskDialogCommonButtons.Close,
                            VerificationText = Resources.MsgDoNotShowAgain
                        }));
                    Setting.ShowStartupConfigurationWarning.Value =
                        !msg.Response.VerificationChecked.GetValueOrDefault();
                }
                else if (App.DatabaseDirectoryUserSpecified)
                {
                    var msg = Messenger.GetResponseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = AppInitResources.MsgDatabasePathWarningTitle,
                            MainIcon = VistaTaskDialogIcon.Warning,
                            MainInstruction = AppInitResources.MsgDatabasePathWarningInst,
                            Content = AppInitResources.MsgDatabasePathWarningContent,
                            FooterIcon = VistaTaskDialogIcon.Error,
                            FooterText = AppInitResources.MsgDatabasePathWarningFooter,
                            CommonButtons = TaskDialogCommonButtons.Close,
                            VerificationText = Resources.MsgDoNotShowAgain
                        }));
                    Setting.ShowStartupConfigurationWarning.Value =
                        !msg.Response.VerificationChecked.GetValueOrDefault();
                }
            }

            Task.Run(() => App.RaiseUserInterfaceReady());

            // initially focus to timeline
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);

            PostInitialize();
        }

        private void PostInitialize()
        {
            if (Setting.CheckDesktopHeap.Value)
            {
                try
                {
                    var dh = SystemInformation.DesktopHeapSize;
                    var rh = App.LeastDesktopHeapSize;
                    if (dh < rh)
                    {
                        var msg = Messenger.GetResponseSafe(() =>
                            new TaskDialogMessage(new TaskDialogOptions
                            {
                                Title = Resources.AppName,
                                MainIcon = VistaTaskDialogIcon.Warning,
                                MainInstruction = AppInitResources.MsgDesktopHeapInst,
                                Content = AppInitResources.MsgDesktopHeapContent,
                                ExpandedInfo = AppInitResources.MsgDesktopHeapInfoFormat.SafeFormat(dh, rh),
                                CommandButtons = new[]
                                    { AppInitResources.MsgButtonBrowseMsKb, Resources.MsgButtonCancel },
                                VerificationText = Resources.MsgDoNotShowAgain
                            }));
                        Setting.CheckDesktopHeap.Value = !msg.Response.VerificationChecked.GetValueOrDefault();
                        if (msg.Response.CommandButtonResult == 0)
                        {
                            BrowserHelper.Open("http://support.microsoft.com/kb/947246");
                        }
                    }
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("sysinfo failed", ex));
                }
            }
        }

        private void ReInitTabs()
        {
            var response = Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgTabWarningTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = AppInitResources.MsgTabWarningInst,
                    Content = AppInitResources.MsgTabWarningContent,
                    CommonButtons = TaskDialogCommonButtons.YesNo
                }));
            if (response.Response.Result != TaskDialogSimpleResult.Yes) return;
            Setting.ResetTabInfo();
            TabManager.Columns.Clear();
            // refresh tabs
            TabManager.Load();
            TabManager.Save();
        }

        public bool OnClosing()
        {
            if (Setting.ConfirmOnExitApp.Value && !MainWindowModel.SuppressCloseConfirmation)
            {
                var ret = Messenger.GetResponseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = AppInitResources.MsgExitTitle,
                        MainIcon = VistaTaskDialogIcon.Warning,
                        MainInstruction = AppInitResources.MsgExitInst,
                        CommonButtons = TaskDialogCommonButtons.OKCancel,
                        VerificationText = Resources.MsgDoNotShowAgain
                    }));
                if (ret.Response == null) return true;
                Setting.ConfirmOnExitApp.Value = !ret.Response.VerificationChecked.GetValueOrDefault();
                if (ret.Response.Result == TaskDialogSimpleResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        #region Status control

        public bool IsMuted => !Setting.PlaySounds.Value;

        public void ToggleMute()
        {
            Setting.PlaySounds.Value = !Setting.PlaySounds.Value;
            RaisePropertyChanged(() => IsMuted);
        }

        public string StateString => MainWindowModel.StateString;

        public string TweetsPerMinutes => StatisticsService.TweetsPerMinutes.ToString(CultureInfo.InvariantCulture);

        public int GrossTweetCount => StatisticsService.EstimatedGrossTweetCount;

        public string StartupTime
        {
            get
            {
                var duration = DateTime.Now - App.StartupDateTime;
                if (duration.TotalHours >= 1)
                {
                    return (int)duration.TotalHours + ":" + duration.ToString("mm\\:ss");
                }
                return duration.ToString("mm\\:ss");
            }
        }

        private void UpdateStatistics()
        {
            RaisePropertyChanged(() => TweetsPerMinutes);
            RaisePropertyChanged(() => GrossTweetCount);
            RaisePropertyChanged(() => StartupTime);
        }

        public void ExecuteGC()
        {
            GC.Collect();
        }

        #endregion Status control

        #region Theme control

        public Uri BackgroundImageUri
        {
            get
            {
                var path = Setting.BackgroundImagePath.Value;
                if (String.IsNullOrEmpty(path))
                {
                    return null;
                }
                if (!Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(path);
                }
                if (File.Exists(path))
                {
                    return new Uri(path, UriKind.Absolute);
                }
                return null;
            }
        }

        public BitmapImage BackgroundImage
        {
            get
            {
                var uri = BackgroundImageUri;
                if (uri == null)
                {
                    return null;
                }
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.CreateOptions = BitmapCreateOptions.None;
                    bi.UriSource = uri;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
                catch (Exception ex)
                {
                    BehaviorLogger.Log("Wallpaper",
                        "Fail to load image: " + Environment.NewLine + ex);
                    return null;
                }
            }
        }

        public double BackgroundImageOpacity => (255 - Math.Min(255, Setting.BackgroundImageTransparency.Value)) /
                                                255.0;

        #endregion Theme control

        #region Toggle Backstage display

        private void TransitionBackstage(bool show)
        {
            if (IsBackstageVisible == show) return;
            ToggleShowBackstage();
        }

        public bool IsBackstageVisible
        {
            get => _isBackstageVisible;
            set
            {
                _isBackstageVisible = value;
                RaisePropertyChanged();
            }
        }

        public void ToggleShowBackstage()
        {
            IsBackstageVisible = !IsBackstageVisible;
            ShowWindowCommands = !IsBackstageVisible;
        }

        #endregion Toggle Backstage display

        #region ShowSettingCommand

        private Livet.Commands.ViewModelCommand _showSettingCommand;
        private bool _isBackstageVisible;

        public Livet.Commands.ViewModelCommand ShowSettingCommand => _showSettingCommand ??
                                                                     (_showSettingCommand =
                                                                         new Livet.Commands.ViewModelCommand(
                                                                             ShowSetting));

        public async void ShowSetting()
        {
            MainWindowModel.SuppressKeyAssigns = true;
            MainWindowModel.SetShowMainWindowCommands(false);
            await MainWindowModel.ShowSetting().DefaultIfEmpty(Unit.Default);
            MainWindowModel.SetShowMainWindowCommands(true);
            MainWindowModel.SuppressKeyAssigns = false;
        }

        #endregion ShowSettingCommand
    }
}