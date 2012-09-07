using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using StarryEyes.Mystique.Models.Connection.UserDependency;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;
using StarryEyes.Mystique.ViewModels.Dialogs;
using StarryEyes.Mystique.Views.Dialogs;
using StarryEyes.Mystique.Filters.Parsing;
using StarryEyes.SweetLady.DataModel;
using System.Collections.Generic;
using StarryEyes.Mystique.Models.Operations;
using StarryEyes.Mystique.Views.Messaging;

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
            if (Setting.Accounts.Count() > 0)
            {
                UserBaseConnectionsManager.Update();
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

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                _query = value;
                RaisePropertyChanged(() => Query);
            }
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
                Setting.Accounts = Setting.Accounts.Append(
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
            int _count = 0;
            try
            {
                var filter = QueryCompiler.Compile(_query);
                var func = filter.GetEvaluator();
                System.Diagnostics.Debug.WriteLine(filter.ToQuery());
                List<TwitterStatus> result = new List<TwitterStatus>();
                sw.Start();
                StatusStore.Find(func) // t.Text.Contains("@")) // find contains hashtags
                    .Subscribe(_ =>
                    {
                        _count++;
                        result.Add(_);
                    },
                    () =>
                    {
                        sw.Stop();
                        result.OrderBy(_ => _.CreatedAt).ForEach(_ => System.Diagnostics.Debug.WriteLine(_));
                        QueryResult = "Completed! (" + sw.Elapsed.TotalSeconds.ToString("0.00") + " sec, " + _count + " records hot.)";
                    });
            }
            catch (Exception ex)
            {
                QueryResult = ex.ToString();
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private string _postText = "";
        public string PostText
        {
            get { return _postText; }
            set
            {
                _postText = value;
                RaisePropertyChanged(() => PostText);
                RaisePropertyChanged(() => PostTextLength);
                if (IsAutoEscapeEnabled)
                    AutoEscape();
            }
        }

        private void AutoEscape()
        {
            var newText = StarryEyes.Mystique.Models.Post.PostUtil.AutoEscape(PostText);
            if (newText != PostText)
                PostText = newText;
        }

        public int PostTextLength
        {
            get { return StarryEyes.Mystique.Models.Post.PostUtil.CountText(PostText); }
        }

        private bool _isAutoEscapeEnabled = false;
        public bool IsAutoEscapeEnabled
        {
            get { return _isAutoEscapeEnabled; }
            set
            {
                _isAutoEscapeEnabled = value;
                RaisePropertyChanged(() => IsAutoEscapeEnabled);
                if (value)
                    AutoEscape();
            }
        }

        #region PostCommand
        private ViewModelCommand _PostCommand;

        public ViewModelCommand PostCommand
        {
            get
            {
                if (_PostCommand == null)
                {
                    _PostCommand = new ViewModelCommand(Post);
                }
                return _PostCommand;
            }
        }

        public void Post()
        {
            var tweetop = new TweetOperation();
            tweetop.Status = PostText;
            PostText = String.Empty;
            tweetop.AuthInfo = Setting.Accounts.First().AuthenticateInfo;
            tweetop.Run().Subscribe(_ => this.Messenger.Raise(new TaskDialogMessage(
                new TaskDialogInterop.TaskDialogOptions()
                {
                    Title = "Tweeted!",
                    MainInstruction = "Tweeted successfully.",
                    ExpandedInfo = _.ToString(),
                    CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.Close
                })),
                ex => this.Messenger.Raise(new TaskDialogMessage(
                    new TaskDialogInterop.TaskDialogOptions()
                    {
                        Title = "Tweet Failed",
                        MainInstruction = "Tweet is failed: " + ex.Message,
                        ExpandedInfo = ex.ToString(),
                        CommonButtons = TaskDialogInterop.TaskDialogCommonButtons.Close
                    })));
        }
        #endregion

    }
}
