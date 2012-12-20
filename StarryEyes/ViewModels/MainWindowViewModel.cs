using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Connections.UserDependencies;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.ViewModels.WindowParts;
using StarryEyes.Views.Dialogs;
using StarryEyes.Views.Helpers;
using StarryEyes.Views.Messaging;

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
        public BackpanelViewModel BackpanelViewModel
        {
            get { return _backpanelViewModel; }
        }

        private readonly InputAreaViewModel _inputAreaViewModel;
        public InputAreaViewModel InputAreaViewModel
        {
            get { return _inputAreaViewModel; }
        }

        private readonly MainAreaViewModel _mainAreaViewModel;
        public MainAreaViewModel MainAreaViewModel
        {
            get { return _mainAreaViewModel; }
        }

        public AccountSelectorViewModel InputAreaAccountSelectorViewModel
        {
            get { return _inputAreaViewModel.AccountSelector; }
        }

        private readonly AccountSelectorViewModel _globalAccountSelectorViewModel;
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
            this.CompositeDisposable.Add(_backpanelViewModel = new BackpanelViewModel());
            this.CompositeDisposable.Add(_inputAreaViewModel = new InputAreaViewModel());
            this.CompositeDisposable.Add(_mainAreaViewModel = new MainAreaViewModel());
            this.CompositeDisposable.Add(_globalAccountSelectorViewModel = new AccountSelectorViewModel());
            _backpanelViewModel.Initialize();
        }

        public void Initialize()
        {
            MainWindowModel.OnWindowCommandDisplayChanged += _ =>
                this.ShowWindowCommands = _;

            this.CompositeDisposable.Add(Observable.FromEvent(
                handler => MainWindowModel.OnStateStringChanged += handler,
                handler => MainWindowModel.OnStateStringChanged -= handler)
                .Subscribe(_ => RaisePropertyChanged(() => StateString)));
            this.CompositeDisposable.Add(Observable.FromEvent(
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
                Observable.FromEvent(_ => _globalAccountSelectorViewModel.OnClosed += _, _ => _globalAccountSelectorViewModel.OnClosed -= _)
                    .Subscribe(_ => aftercall(_globalAccountSelectorViewModel.SelectedAccounts));
                _globalAccountSelectorViewModel.Open();
            };

            if (Setting.IsFirstGenerated)
            {
                var kovm = new KeyOverrideViewModel();
                this.Messenger.RaiseAsync(new TransitionMessage(
                    typeof(KeyOverrideWindow),
                    kovm, TransitionMode.Modal, null));
            }

            // Start receiving
            if (AccountsStore.Accounts.Count() > 0)
            {
                UserBaseConnectionsManager.Update();
            }
            else
            {
                var auth = new AuthorizationViewModel();
                auth.AuthorizeObservable.Subscribe(_ =>
                {
                    AccountsStore.Accounts.Add(
                        new AccountSetting()
                        {
                            AuthenticateInfo = _,
                            IsUserStreamsEnabled = true
                        });
                    UserBaseConnectionsManager.Update();
                });
                this.Messenger.RaiseAsync(new TransitionMessage(
                    typeof(AuthorizationWindow),
                    auth, TransitionMode.Modal, null));
            }
            TabManager.CreateTab(new TabModel("hello", "from all where ()"));
            TabManager.CreateTab(new TabModel("home", "from all where user <- *.following"));
            TabManager.CreateTab(new TabModel("replies", "from all where to -> *"));
            TabManager.CreateTab(new TabModel("Krile", "from all where source == \"Krile\""));
        }

        public bool OnClosing()
        {
            if (Setting.ConfirmOnExitApp.Value)
            {
                var ret = this.Messenger.GetResponse(
                    new TaskDialogMessage(new TaskDialogInterop.TaskDialogOptions()
                    {
                        Title = "Krileの終了",
                        MainIcon = TaskDialogInterop.VistaTaskDialogIcon.Warning,
                        MainInstruction = "Krileを終了してもよろしいですか？",
                        VerificationText = "次回から確認せずに終了",
                        CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.OKCancel,
                    }));
                if (ret.Response.VerificationChecked.GetValueOrDefault())
                {
                    Setting.ConfirmOnExitApp.Value = false;
                }
                if (ret.Response.Result == TaskDialogInterop.TaskDialogSimpleResult.Cancel)
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

        private void UpdateStatistics()
        {
            RaisePropertyChanged(() => TweetsPerMinutes);
            RaisePropertyChanged(() => GrossTweetCount);
            RaisePropertyChanged(() => StartupTime);
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
                var duration = DateTime.Now - App.StartupDateTime;
                StringBuilder builder = new StringBuilder();
                if (duration.Hours > 0)
                {
                    return duration.Hours + ":" + duration.ToString("mm\\:ss");
                }
                else
                {
                    return duration.ToString("mm\\:ss");
                }
            }
        }

        #endregion
    }
}
