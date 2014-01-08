using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using Livet.Messaging;
using StarryEyes.Annotations;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.ViewModels.WindowParts.Flips;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        #region Included viewmodels

        private readonly BackstageViewModel _backstageViewModel;

        private readonly AccountSelectionFlipViewModel _globalAccountSelectionFlipViewModel;

        private readonly InputAreaViewModel _inputAreaViewModel;

        private readonly MainAreaViewModel _mainAreaViewModel;

        private readonly SettingFlipViewModel _settingFlipViewModel;

        private readonly TabConfigurationFlipViewModel _tabConfigurationFlipViewModel;

        private readonly SearchFlipViewModel _searchFlipViewModel;

        public BackstageViewModel BackstageViewModel
        {
            get { return this._backstageViewModel; }
        }

        public InputAreaViewModel InputAreaViewModel
        {
            get { return _inputAreaViewModel; }
        }

        public MainAreaViewModel MainAreaViewModel
        {
            get { return _mainAreaViewModel; }
        }

        public AccountSelectionFlipViewModel InputAreaAccountSelectionFlipViewModel
        {
            get { return _inputAreaViewModel.AccountSelectionFlip; }
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
            CompositeDisposable.Add(_inputAreaViewModel = new InputAreaViewModel());
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
                _ => RaisePropertyChanged(() => BackgroundImageUri)));
            CompositeDisposable.Add(Setting.BackgroundImageTransparency.ListenValueChanged(
                _ => RaisePropertyChanged(() => BackgroundImageOpacity)));
            CompositeDisposable.Add(Setting.RotateWindowContent.ListenValueChanged(
                _ => RaisePropertyChanged(() => RotateWindowContent)));
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
                    InputAreaViewModel.OpenInput();
                    InputAreaViewModel.FocusToTextBox();
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
                          .Subscribe(options => this.Messenger.Raise(new TaskDialogMessage(options))));
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
            #endregion

            #region special navigations
            // check first boot
            if (Setting.IsFirstGenerated)
            {
                var kovm = new KeyOverrideViewModel();
                Messenger.Raise(new TransitionMessage(
                                         typeof(KeyOverrideWindow),
                                         kovm, TransitionMode.Modal, null));
            }

            // register new account if accounts haven't been authorized yet
            if (!Setting.Accounts.Collection.Any())
            {
                var auth = new AuthorizationViewModel();
                auth.AuthorizeObservable
                    .Subscribe(Setting.Accounts.Collection.Add);
                Messenger.RaiseAsync(new TransitionMessage(
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
                        var msg = this.Messenger.GetResponse(new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = "Krile StarryEyes",
                            MainIcon = VistaTaskDialogIcon.Warning,
                            MainInstruction = "メモリ不足に陥る可能性があります。",
                            Content = "Krileのクラッシュやシステムの停止を引き起こす可能性があります。" + Environment.NewLine +
                                      "Windowsの設定を変更することで、この問題を回避できる可能性があります。",
                            ExpandedInfo = "デスクトップ ヒープの設定が少なすぎます。" + Environment.NewLine +
                                           "現在の設定値: " + dh + " / 推奨下限設定値: " + rh,
                            CommandButtons = new[] { "Microsoft KBを参照", "キャンセル" },
                            VerificationText = "次回から表示しない"
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
            var response = this.Messenger.GetResponse(new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "タブ情報の警告",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "タブ情報が失われた可能性があります。",
                Content = "タブが空です。" + Environment.NewLine +
                          "初回起動時に生成されるタブを再生成しますか？",
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
                var ret = Messenger.GetResponse(
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = "Krileの終了",
                        MainIcon = VistaTaskDialogIcon.Warning,
                        MainInstruction = "Krileを終了してもよろしいですか？",
                        CommonButtons = TaskDialogCommonButtons.OKCancel,
                        VerificationText = "次回から確認せずに終了",
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

        public string StateString
        {
            get { return MainWindowModel.StateString; }
        }

        public string TweetsPerMinutes
        {
            get { return (StatisticsService.TweetsPerMinutes).ToString(CultureInfo.InvariantCulture); }
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

        public double BackgroundImageOpacity
        {
            get { return (255 - Math.Min(255, Setting.BackgroundImageTransparency.Value)) / 255.0; }
        }

        public bool RotateWindowContent
        {
            get { return Setting.RotateWindowContent.Value; }
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