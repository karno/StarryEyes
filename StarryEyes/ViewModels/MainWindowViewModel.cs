using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using Livet.Messaging;
using StarryEyes.Models;
using StarryEyes.Models.Connections.UserDependencies;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts;
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
        private readonly AccountSelectorViewModel _globalAccountSelectorViewModel;

        private readonly InputAreaViewModel _inputAreaViewModel;

        private readonly MainAreaViewModel _mainAreaViewModel;

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

        public AccountSelectorViewModel InputAreaAccountSelectorViewModel
        {
            get { return _inputAreaViewModel.AccountSelector; }
        }

        public AccountSelectorViewModel GlobalAccountSelectorViewModel
        {
            get { return _globalAccountSelectorViewModel; }
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
            CompositeDisposable.Add(_globalAccountSelectorViewModel = new AccountSelectorViewModel());
            _backpanelViewModel.Initialize();
        }

        public void Initialize()
        {
            MainWindowModel.OnWindowCommandDisplayChanged += _ =>
                                                             ShowWindowCommands = _;

            CompositeDisposable.Add(Observable.FromEvent(
                handler => MainWindowModel.OnStateStringChanged += handler,
                handler => MainWindowModel.OnStateStringChanged -= handler)
                                              .Subscribe(_ => RaisePropertyChanged(() => StateString)));
            CompositeDisposable.Add(Observable.FromEvent(
                handler => StatisticsService.OnStatisticsParamsUpdated += handler,
                handler => StatisticsService.OnStatisticsParamsUpdated -= handler)
                                              .Subscribe(_ => UpdateStatistics()));

            MainWindowModel.OnExecuteAccountSelectActionRequested += (action, status, selecteds, aftercall) =>
                {
                    _globalAccountSelectorViewModel.SelectedAccounts = selecteds;
                    _globalAccountSelectorViewModel.SelectionReason = "";
                    switch (action)
                    {
                        case AccountSelectionAction.Favorite:
                            _globalAccountSelectorViewModel.SelectionReason = "favorite";
                            break;
                        case AccountSelectionAction.Retweet:
                            _globalAccountSelectorViewModel.SelectionReason = "retweet";
                            break;
                    }
                    Observable.FromEvent(_ => _globalAccountSelectorViewModel.OnClosed += _,
                                         _ => _globalAccountSelectorViewModel.OnClosed -= _)
                              .Subscribe(_ => aftercall(_globalAccountSelectorViewModel.SelectedAccounts));
                    _globalAccountSelectorViewModel.Open();
                };

            if (Setting.IsFirstGenerated)
            {
                var kovm = new KeyOverrideViewModel();
                Messenger.RaiseAsync(new TransitionMessage(
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
            TabManager.CreateTab(new TabModel("hello", "from all where ()"));
            TabManager.CreateTab(new TabModel("home", "from all where user <- *.following"));
            TabManager.CreateTab(new TabModel("replies", "from all where to -> *"));
            TabManager.CreateTab(new TabModel("Krile", "from all where source == \"Krile\""));
            TabManager.CreateColumn(new TabModel("Favorites", "from all where user <- * && ( favs > 0 || rts > 0)"));
        }

        public bool OnClosing()
        {
            if (Setting.ConfirmOnExitApp.Value)
            {
                var ret = Messenger.GetResponse(
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
                if (duration.Hours > 0)
                {
                    return duration.Hours + ":" + duration.ToString("mm\\:ss");
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

        public static void Bomb()
        {
            StatusStore.Find(s => s.User.ScreenName == "karno").ふぁぼ();
            StatusStore.Find(s => s.User.ScreenName == "karno").ﾘﾂｲｰｮ();
            StatusStore.Find(s => s.User.ScreenName == "karno").ふぁぼ公();
        }
    }

    public static class 爆撃Extensions
    {
        public static void ふぁぼ公(this IObservable<StarryEyes.Breezy.DataModel.TwitterStatus> statuses)
        {
            statuses.Publish(observable =>
            {
                observable.ふぁぼ();
                observable.ﾘﾂｲｰｮ();
                return observable;
            })
            .Subscribe();
        }

        public static void ふぁぼ(this IObservable<StarryEyes.Breezy.DataModel.TwitterStatus> statuses)
        {
            statuses.SelectMany(s => AccountsStore.Accounts
                        .Select(a => new StarryEyes.Models.Operations.FavoriteOperation(a.AuthenticateInfo, s, true)))
                    .SelectMany(_ => _.Run())
                    .Subscribe();
        }

        public static void ﾘﾂｲｰｮ(this IObservable<StarryEyes.Breezy.DataModel.TwitterStatus> statuses)
        {
            statuses.SelectMany(s => AccountsStore.Accounts
                        .Select(a => new StarryEyes.Models.Operations.RetweetOperation(a.AuthenticateInfo, s, true)))
                    .SelectMany(_ => _.Run())
                    .Subscribe();
        }

    }
}