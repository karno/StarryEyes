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

        private readonly BackstageViewModel _backstageViewModel;

        private readonly AccountSelectionFlipViewModel _globalAccountSelectionFlipViewModel;

        private readonly InputViewModel _inputViewModel;

        private readonly MainAreaViewModel _mainAreaViewModel;

        private readonly SettingFlipViewModel _settingFlipViewModel;

        private readonly TabConfigurationFlipViewModel _tabConfigurationFlipViewModel;

        private readonly SearchFlipViewModel _searchFlipViewModel;

        public BackstageViewModel BackstageViewModel
        {
            get { return this._backstageViewModel; }
        }

        public InputViewModel InputViewModel
        {
            get { return this._inputViewModel; }
        }

        public MainAreaViewModel MainAreaViewModel
        {
            get { return _mainAreaViewModel; }
        }

        public AccountSelectionFlipViewModel InputAreaAccountSelectionFlipViewModel
        {
            get { return this._inputViewModel.AccountSelectorViewModel.AccountSelectionFlip; }
        }

        public AccountSelectionFlipViewModel GlobalAccountSelectionFlipViewModel
        {
            get { return _globalAccountSelectionFlipViewModel; }
        }

        public SettingFlipViewModel SettingFlipViewModel
        {
            get { return _settingFlipViewModel; }
        }

        public TabConfigurationFlipViewModel TabConfigurationFlipViewModel
        {
            get { return _tabConfigurationFlipViewModel; }
        }

        public SearchFlipViewModel SearchFlipViewModel
        {
            get { return _searchFlipViewModel; }
        }

        #endregion

        #region Properties

        public string WindowTitle
        {
            get
            {
                return !App.IsUnstableVersion ? App.AppFullName : App.AppFullName + " " + App.FormattedVersion;
            }
        }

        private bool _showWindowCommands = true;

        public bool ShowWindowCommands
        {
            get { return _showWindowCommands; }
            set
            {
                _showWindowCommands = value;
                RaisePropertyChanged(() => ShowWindowCommands);
            }
        }

        public bool ShowSettingLabel
        {
            get { return _showWindowCommands && SearchFlipViewModel.IsVisible; }
        }

        public bool ShowSearchBox
        {
            get { return _showWindowCommands; }
        }

        #endregion

        public MainWindowViewModel()
        {
            CompositeDisposable.Add(_backstageViewModel = new BackstageViewModel());
            CompositeDisposable.Add(this._inputViewModel = new InputViewModel());
            CompositeDisposable.Add(_mainAreaViewModel = new MainAreaViewModel());
            CompositeDisposable.Add(_globalAccountSelectionFlipViewModel = new AccountSelectionFlipViewModel());
            CompositeDisposable.Add(_settingFlipViewModel = new SettingFlipViewModel(this));
            CompositeDisposable.Add(_tabConfigurationFlipViewModel = new TabConfigurationFlipViewModel());
            CompositeDisposable.Add(_searchFlipViewModel = new SearchFlipViewModel());
            CompositeDisposable.Add(Observable
                .FromEvent<FocusRequest>(
                    h => MainWindowModel.FocusRequested += h,
                    h => MainWindowModel.FocusRequested -= h)
                .Subscribe(SetFocus));
            CompositeDisposable.Add(Observable
                .FromEvent<bool>(
                    h => MainWindowModel.BackstageTransitionRequested += h,
                    h => MainWindowModel.BackstageTransitionRequested -= h)
                .Subscribe(this.TransitionBackstage));
            CompositeDisposable.Add(Setting.BackgroundImagePath.ListenValueChanged(
                _ =>
                {
                    RaisePropertyChanged(() => BackgroundImageUri);
                    RaisePropertyChanged(() => BackgroundImage);
                }));
            CompositeDisposable.Add(Setting.BackgroundImageTransparency.ListenValueChanged(
                _ => RaisePropertyChanged(() => BackgroundImageOpacity)));
            this._backstageViewModel.Initialize();
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
                    if (ctab != null)
                    {
                        ctab.RequestFocus();
                    }
                    break;
                case FocusRequest.Input:
                    SearchFlipViewModel.Close();
                    this.InputViewModel.OpenInput();
                    this.InputViewModel.FocusToTextBox();
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
                          .Subscribe(options => this.Messenger.RaiseSafe(() =>
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
                                  _globalAccountSelectionFlipViewModel.Close();

                                  _globalAccountSelectionFlipViewModel.SelectedAccounts =
                                      desc.SelectionAccounts;
                                  _globalAccountSelectionFlipViewModel.SelectionReason = "";
                                  switch (desc.AccountSelectionAction)
                                  {
                                      case AccountSelectionAction.Favorite:
                                          _globalAccountSelectionFlipViewModel.SelectionReason =
                                              "favorite";
                                          break;
                                      case AccountSelectionAction.Retweet:
                                          _globalAccountSelectionFlipViewModel.SelectionReason =
                                              "retweet";
                                          break;
                                  }
                                  IDisposable disposable = null;
                                  disposable = Observable.FromEvent(
                                      h => _globalAccountSelectionFlipViewModel.Closed += h,
                                      h => _globalAccountSelectionFlipViewModel.Closed -= h)
                                                         .Subscribe(_ =>
                                                         {
                                                             if (disposable == null) return;
                                                             disposable.Dispose();
                                                             disposable = null;
                                                             desc.Callback(
                                                                 this._globalAccountSelectionFlipViewModel
                                                                     .SelectedAccounts);
                                                         });
                                  _globalAccountSelectionFlipViewModel.Open();
                              }));
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => ThemeManager.ThemeChanged += h,
                    h => ThemeManager.ThemeChanged -= h
                    ).Subscribe(_ => this.Messenger.RaiseAsync(new InteractionMessage("InvalidateTheme"))));

            #endregion

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
            #endregion

            TabManager.Load();
            TabManager.Save();

            if (TabManager.Columns.Count == 1 && TabManager.Columns[0].Tabs.Count == 0)
            {
                // lost tab info
                this.ReInitTabs();
            }

            // check cleanup parameter
            if (Setting.AutoCleanupTweets.Value &&
                Setting.AutoCleanupThreshold.Value < 0)
            {
                var msg = this.Messenger.GetResponseSafe(() =>
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
                    var msg = this.Messenger.GetResponseSafe(() =>
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
                    Setting.ShowStartupConfigurationWarning.Value = !msg.Response.VerificationChecked.GetValueOrDefault();
                }
                else if (App.DatabaseDirectoryUserSpecified)
                {
                    var msg = this.Messenger.GetResponseSafe(() =>
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
                    Setting.ShowStartupConfigurationWarning.Value = !msg.Response.VerificationChecked.GetValueOrDefault();
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
                        var msg = this.Messenger.GetResponseSafe(() =>
                            new TaskDialogMessage(new TaskDialogOptions
                            {
                                Title = Resources.AppName,
                                MainIcon = VistaTaskDialogIcon.Warning,
                                MainInstruction = AppInitResources.MsgDesktopHeapInst,
                                Content = AppInitResources.MsgDesktopHeapContent,
                                ExpandedInfo = AppInitResources.MsgDesktopHeapInfoFormat.SafeFormat(dh, rh),
                                CommandButtons = new[] { AppInitResources.MsgButtonBrowseMsKb, Resources.MsgButtonCancel },
                                VerificationText = Resources.MsgDoNotShowAgain,
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
            var response = this.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = AppInitResources.MsgTabWarningTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = AppInitResources.MsgTabWarningInst,
                    Content = AppInitResources.MsgTabWarningContent,
                    CommonButtons = TaskDialogCommonButtons.YesNo,
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
                        VerificationText = Resources.MsgDoNotShowAgain,
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

        public bool IsMuted
        {
            get { return !Setting.PlaySounds.Value; }
        }

        public void ToggleMute()
        {
            Setting.PlaySounds.Value = !Setting.PlaySounds.Value;
            RaisePropertyChanged(() => IsMuted);
        }

        public string StateString
        {
            get { return MainWindowModel.StateString; }
        }

        public string TweetsPerMinutes
        {
            get { return StatisticsService.TweetsPerMinutes.ToString(CultureInfo.InvariantCulture); }
        }

        public int GrossTweetCount
        {
            get { return StatisticsService.EstimatedGrossTweetCount; }
        }

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

        #endregion

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

        public double BackgroundImageOpacity
        {
            get { return (255 - Math.Min(255, Setting.BackgroundImageTransparency.Value)) / 255.0; }
        }

        #endregion

        #region Toggle Backstage display

        private void TransitionBackstage(bool show)
        {
            if (IsBackstageVisible == show) return;
            this.ToggleShowBackstage();
        }

        public bool IsBackstageVisible
        {
            get { return this._isBackstageVisible; }
            set
            {
                this._isBackstageVisible = value;
                this.RaisePropertyChanged();
            }
        }

        public void ToggleShowBackstage()
        {
            IsBackstageVisible = !IsBackstageVisible;
            ShowWindowCommands = !IsBackstageVisible;
        }

        #endregion

        #region ShowSettingCommand
        private Livet.Commands.ViewModelCommand _showSettingCommand;
        private bool _isBackstageVisible;

        public Livet.Commands.ViewModelCommand ShowSettingCommand
        {
            get
            {
                return _showSettingCommand ??
                       (_showSettingCommand = new Livet.Commands.ViewModelCommand(ShowSetting));
            }
        }

        public async void ShowSetting()
        {
            MainWindowModel.SuppressKeyAssigns = true;
            MainWindowModel.SetShowMainWindowCommands(false);
            await MainWindowModel.ShowSetting().DefaultIfEmpty(Unit.Default);
            MainWindowModel.SetShowMainWindowCommands(true);
            MainWindowModel.SuppressKeyAssigns = false;
        }

        #endregion
    }
}