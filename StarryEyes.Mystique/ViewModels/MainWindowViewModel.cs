using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using StarryEyes.Mystique.Views.Dialogs;
using StarryEyes.Mystique.ViewModels.Dialogs;
using System.Reactive.Linq;
using StarryEyes.Mystique.Settings;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.Mystique.Models.Connection;
using System.Diagnostics;
using StarryEyes.Mystique.Models.Connection.UserDependency;

namespace StarryEyes.Mystique.ViewModels
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
        public void Initialize()
        {
            StatisticsHub.OnStatisticsParamsUpdated += () =>
            {
                RaisePropertyChanged(() => StatusCount);
                RaisePropertyChanged(() => TweetsPerMinutes);
            };
            if (Setting.Accounts.Value.Count > 0)
            {
                UserDependencyConnectionsManager.Update();
            }
            StatusStore.StatusPublisher
                .Where(_ => _.IsAdded)
                .Select(_ => _.Status)
                .Subscribe(_ => RecentReceivedBody = _.Text);
        }

        private string _recentReceivedBody = String.Empty;
        public string RecentReceivedBody
        {
            get { return _recentReceivedBody; }
            set
            {
                _recentReceivedBody = value;
                RaisePropertyChanged(() => RecentReceivedBody);
            }
        }

        public int StatusCount
        {
            get { return StatisticsHub.EstimatedGrossTweetCount; }
        }

        public double TweetsPerMinutes
        {
            get { return StatisticsHub.TweetsPerSeconds * 60; }
        }

        private string _queryResult;
        public string QueryResult
        {
            get { return _queryResult; }
            set
            {
                _queryResult = value;
                RaisePropertyChanged(() => QueryResult);
            }
        }

        #region StartReceiveCommand
        private ViewModelCommand _StartReceiveCommand;

        public ViewModelCommand StartReceiveCommand
        {
            get
            {
                if (_StartReceiveCommand == null)
                {
                    _StartReceiveCommand = new ViewModelCommand(StartReceive);
                }
                return _StartReceiveCommand;
            }
        }

        public void StartReceive()
        {
            var auth = new AuthorizationViewModel();
            auth.AuthorizeObservable.Subscribe(_ =>
            {
                Setting.Accounts.Value.Add(new AccountSetting()
                {
                    AuthenticateInfo = _,
                    IsUserStreamsEnabled = true
                });
                UserDependencyConnectionsManager.Update();
            });
            this.Messenger.RaiseAsync(new TransitionMessage(
                typeof(AuthorizationWindow),
                auth, TransitionMode.Modal, null));
        }
        #endregion

        #region ExecuteFilterCommand
        private ViewModelCommand _ExecuteFilterCommand;

        public ViewModelCommand ExecuteFilterCommand
        {
            get
            {
                if (_ExecuteFilterCommand == null)
                {
                    _ExecuteFilterCommand = new ViewModelCommand(ExecuteFilter);
                }
                return _ExecuteFilterCommand;
            }
        }

        public void ExecuteFilter()
        {
            QueryResult = "querying...";
            var sw = new Stopwatch();
            sw.Start();
            int _count = 0;
            var rv = new StarryEyes.Mystique.Filters.Expressions.Operators.FilterOperatorPlus();
            rv.LeftValue =  new StarryEyes.Mystique.Filters.Expressions.Values.Statuses.UserStatuses();
            rv.RightValue = new StarryEyes.Mystique.Filters.Expressions.Values.Immediates.NumericValue(100000);
            var op = new StarryEyes.Mystique.Filters.Expressions.Operators.FilterOperatorGreaterThan();
            op.LeftValue = new StarryEyes.Mystique.Filters.Expressions.Values.Statuses.UserFavroites();
            op.RightValue = rv;
            System.Diagnostics.Debug.WriteLine(op.ToQuery());
            // op.RightValue = new StarryEyes.Mystique.Filters.Expressions.Values.Immediates.NumericValue(10000);
            var func = op.GetBooleanValueProvider();
            StatusStore.Find(func) // t.Text.Contains("@")) // find contains hashtags
                .Subscribe(_ =>
                {
                    _count++;
                    System.Diagnostics.Debug.WriteLine(_.ToString() + " / " + _.User.FavoritesCount);
                },
                () =>
                {
                    sw.Stop();
                    QueryResult = "Completed! (" + sw.Elapsed.TotalSeconds.ToString("0.00") + " sec, " + _count + " records hot.)";
                });
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
