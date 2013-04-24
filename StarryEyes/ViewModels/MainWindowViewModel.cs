using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Livet;
using Livet.Messaging;
using StarryEyes.Filters.Expressions;
using StarryEyes.Models;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Tab;
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

        private readonly BackpanelViewModel _backpanelViewModel;

        private readonly AccountSelectionFlipViewModel _globalAccountSelectionFlipViewModel;

        private readonly InputAreaViewModel _inputAreaViewModel;

        private readonly MainAreaViewModel _mainAreaViewModel;

        private readonly TabConfigurationFlipViewModel _tabConfigurationFlipViewModel;

        private readonly SearchFlipViewModel _searchFlipViewModel;

        public BackpanelViewModel BackpanelViewModel
        {
            get { return _backpanelViewModel; }
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
            CompositeDisposable.Add(_backpanelViewModel = new BackpanelViewModel());
            CompositeDisposable.Add(_inputAreaViewModel = new InputAreaViewModel());
            CompositeDisposable.Add(_mainAreaViewModel = new MainAreaViewModel());
            CompositeDisposable.Add(_globalAccountSelectionFlipViewModel = new AccountSelectionFlipViewModel());
            CompositeDisposable.Add(_tabConfigurationFlipViewModel = new TabConfigurationFlipViewModel());
            CompositeDisposable.Add(_searchFlipViewModel = new SearchFlipViewModel());
            CompositeDisposable.Add(Observable.FromEvent<FocusRequest>(
                h => MainWindowModel.OnFocusRequested += h,
                h => MainWindowModel.OnFocusRequested -= h)
                .Subscribe(SetFocus));
            CompositeDisposable.Add(Observable.FromEvent<Tuple<string, FilterExpressionBase>>(
                h => MainWindowModel.OnConfirmMuteRequested += h,
                h => MainWindowModel.OnConfirmMuteRequested -= h)
                .Subscribe(OnMuteRequested));
            _backpanelViewModel.Initialize();
        }

        private void SetFocus(FocusRequest req)
        {
            switch (req)
            {
                case FocusRequest.Search:
                    SearchFlipViewModel.FocusToSearchBox();
                    break;
                case FocusRequest.Timeline:
                    TabManager.CurrentFocusTab.SetPhysicalFocus();
                    SearchFlipViewModel.Close();
                    break;
                case FocusRequest.Input:
                    InputAreaViewModel.OpenInput();
                    InputAreaViewModel.FocusToTextBox();
                    SearchFlipViewModel.Close();
                    break;
            }
        }

        private void OnMuteRequested(Tuple<string, FilterExpressionBase> tuple)
        {
        }

        private int _visibleCount;

        public void Initialize()
        {
            MainWindowModel.OnWindowCommandDisplayChanged += visible =>
            {
                int offset = visible ? Interlocked.Increment(ref _visibleCount) : Interlocked.Decrement(ref _visibleCount);
                ShowWindowCommands = offset >= 0;
            };

            CompositeDisposable.Add(Observable.FromEvent(
                h => MainWindowModel.OnStateStringChanged += h,
                h => MainWindowModel.OnStateStringChanged -= h)
                                              .Subscribe(_ => RaisePropertyChanged(() => StateString)));
            CompositeDisposable.Add(Observable.FromEvent(
                h => StatisticsService.OnStatisticsParamsUpdated += h,
                h => StatisticsService.OnStatisticsParamsUpdated -= h)
                                              .Subscribe(_ => UpdateStatistics()));

            MainWindowModel.OnExecuteAccountSelectActionRequested += (action, status, selecteds, aftercall) =>
            {
                _globalAccountSelectionFlipViewModel.SelectedAccounts = selecteds;
                _globalAccountSelectionFlipViewModel.SelectionReason = "";
                switch (action)
                {
                    case AccountSelectionAction.Favorite:
                        _globalAccountSelectionFlipViewModel.SelectionReason = "favorite";
                        break;
                    case AccountSelectionAction.Retweet:
                        _globalAccountSelectionFlipViewModel.SelectionReason = "retweet";
                        break;
                }
                IDisposable disposable = null;
                disposable = Observable.FromEvent(h => _globalAccountSelectionFlipViewModel.OnClosed += h,
                                                  h => _globalAccountSelectionFlipViewModel.OnClosed -= h)
                                       .Subscribe(_ =>
                                       {
                                           if (disposable != null)
                                           {
                                               disposable.Dispose();
                                               disposable = null;
                                               aftercall(_globalAccountSelectionFlipViewModel.SelectedAccounts);
                                           }
                                       });
                _globalAccountSelectionFlipViewModel.Open();
            };

            if (Setting.IsFirstGenerated)
            {
                var kovm = new KeyOverrideViewModel();
                Messenger.Raise(new TransitionMessage(
                                         typeof(KeyOverrideWindow),
                                         kovm, TransitionMode.Modal, null));
            }

            // Start receiving
            if (!AccountsStore.Accounts.Any())
            {
                var auth = new AuthorizationViewModel();
                auth.AuthorizeObservable.Subscribe(
                    _ => AccountsStore.Accounts.Add(
                        new AccountSetting
                        {
                            AuthenticateInfo = _,
                            IsUserStreamsEnabled = true
                        }));
                Messenger.RaiseAsync(new TransitionMessage(
                                         typeof(AuthorizationWindow),
                                         auth, TransitionMode.Modal, null));
            }
            TabManager.Load();
            TabManager.Save();
            if (TabManager.Columns.Count == 1 && TabManager.Columns[0].Tabs.Count == 0)
            {
                var response = this.Messenger.GetResponse(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "タブ情報の警告",
                    CommonButtons = TaskDialogCommonButtons.YesNo,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = "タブ情報が失われた可能性があります。",
                    Content = "タブが空です。" + Environment.NewLine +
                    "初回起動時に生成されるタブを再生成しますか？",
                }));
                if (response.Response.Result == TaskDialogSimpleResult.Yes)
                {
                    Setting.ResetTabInfo();
                    TabManager.Columns.Clear();
                    TabManager.Load();
                    TabManager.Save();
                }
            }
        }

        public bool OnClosing()
        {
            if (Setting.ConfirmOnExitApp.Value)
            {
                TaskDialogMessage ret = Messenger.GetResponse(
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = "Krileの終了",
                        MainIcon = VistaTaskDialogIcon.Warning,
                        MainInstruction = "Krileを終了してもよろしいですか？",
                        VerificationText = "次回から確認せずに終了",
                        CommonButtons = TaskDialogCommonButtons.OKCancel,
                    }));
                if (ret.Response == null) return true;
                if (ret.Response.VerificationChecked.GetValueOrDefault())
                {
                    Setting.ConfirmOnExitApp.Value = false;
                }
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
            get { return (StatisticsService.TweetsPerSeconds * 60).ToString("0.0"); }
        }

        public int GrossTweetCount
        {
            get { return StatisticsService.EstimatedGrossTweetCount; }
        }

        public string StartupTime
        {
            get
            {
                TimeSpan duration = DateTime.Now - App.StartupDateTime;
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

        #region ShowSettingCommand
        private Livet.Commands.ViewModelCommand _showSettingCommand;

        public Livet.Commands.ViewModelCommand ShowSettingCommand
        {
            get { return _showSettingCommand ?? (_showSettingCommand = new Livet.Commands.ViewModelCommand(ShowSetting)); }
        }

        public void ShowSetting()
        {
            MainWindowModel.SetShowMainWindowCommands(false);
            // TODO: show settings.
            MainWindowModel.SetShowMainWindowCommands(true);
        }
        #endregion
    }
}