using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Livet;
using Livet.Messaging;
using StarryEyes.Models;
using StarryEyes.Models.Connections.UserDependencies;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Controls;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.ViewModels.WindowParts.Flips;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Messaging;
using TaskDialogInterop;

namespace StarryEyes.ViewModels
{
    /* コマンド、プロパティの定義にはそれぞれ 
     * 
     *  lvcom   : ViewModelCommand
     *  lvcomn  : ViewModelCommand(CanExecute無)
     *  llcom   : ListenerCommand(パラメータ有のコマンド)
     *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
     *  lprop   : 変更通知プロパティ
     *  
     * を使用してください。
     * 
     * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
     * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
     * LivetCallMethodActionなどから直接メソッドを呼び出してください。
     * 
     * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
     * 同様に直接ViewModelのメソッドを呼び出し可能です。
     */

    /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
     * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
     */

    /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
     * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
     * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
     * 
     * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
     * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
     * 
     * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
     * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
     * 
     * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
     */

    /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
     * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
     * 
     * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
     * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
     */

    public class MainWindowViewModel : ViewModel
    {
        #region Included viewmodels

        private readonly BackpanelViewModel _backpanelViewModel;

        private readonly AccountSelectionFlipViewModel _globalAccountSelectionFlipViewModel;

        private readonly InputAreaViewModel _inputAreaViewModel;

        private readonly MainAreaViewModel _mainAreaViewModel;

        private readonly TabConfigurationFlipViewModel _tabConfigurationFlipViewModel;

        private readonly SearchTextBoxViewModel _searchTextBoxViewModel;

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

        public SearchTextBoxViewModel SearchTextBoxViewModel
        {
            get { return _searchTextBoxViewModel; }
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

        #endregion

        public MainWindowViewModel()
        {
            CompositeDisposable.Add(_backpanelViewModel = new BackpanelViewModel());
            CompositeDisposable.Add(_inputAreaViewModel = new InputAreaViewModel());
            CompositeDisposable.Add(_mainAreaViewModel = new MainAreaViewModel());
            CompositeDisposable.Add(_globalAccountSelectionFlipViewModel = new AccountSelectionFlipViewModel());
            CompositeDisposable.Add(_tabConfigurationFlipViewModel = new TabConfigurationFlipViewModel());
            CompositeDisposable.Add(_searchTextBoxViewModel = new SearchTextBoxViewModel());
            _backpanelViewModel.Initialize();
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
            if (AccountsStore.Accounts.Any())
            {
                UserBaseConnectionsManager.Update();
            }
            else
            {
                var auth = new AuthorizationViewModel();
                auth.AuthorizeObservable.Subscribe(_ =>
                {
                    AccountsStore.Accounts.Add(
                        new AccountSetting
                        {
                            AuthenticateInfo = _,
                            IsUserStreamsEnabled = true
                        });
                    UserBaseConnectionsManager.Update();
                });
                Messenger.RaiseAsync(new TransitionMessage(
                                         typeof(AuthorizationWindow),
                                         auth, TransitionMode.Modal, null));
            }
            MainAreaModel.Load();
            MainAreaModel.Save();
            /*
            MainAreaModel.CreateTab(new TabModel("hello", "from all where ()"));
            MainAreaModel.CreateTab(new TabModel("home", "from all where user <- *.following"));
            MainAreaModel.CreateTab(new TabModel("replies", "from all where to -> *"));
            MainAreaModel.CreateTab(new TabModel("my", "from all where user <- *"));
            MainAreaModel.CreateTab(new TabModel("Krile", "from all where source == \"Krile\""));
            MainAreaModel.CreateColumn(new TabModel("Favorites", "from all where user <- * && ( favs > 0 || rts > 0)"));
            */
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

        public bool IsTooFast
        {
            get { return StatisticsService.TooFastWarning; }
        }

        public void ResetTooFastFlag()
        {
            StatisticsService.ResetTooFastWarning();
        }

        private void UpdateStatistics()
        {
            RaisePropertyChanged(() => TweetsPerMinutes);
            RaisePropertyChanged(() => GrossTweetCount);
            RaisePropertyChanged(() => StartupTime);
            RaisePropertyChanged(() => IsTooFast);
        }

        #endregion

        #region ShowSettingCommand
        private Livet.Commands.ViewModelCommand _ShowSettingCommand;

        public Livet.Commands.ViewModelCommand ShowSettingCommand
        {
            get
            {
                if (_ShowSettingCommand == null)
                {
                    _ShowSettingCommand = new Livet.Commands.ViewModelCommand(ShowSetting);
                }
                return _ShowSettingCommand;
            }
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