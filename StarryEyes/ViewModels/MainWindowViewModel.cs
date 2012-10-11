using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using StarryEyes.Models.Connection.UserDependency;
using StarryEyes.Models.Hub;
using StarryEyes.Models.Store;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Dialogs;
using StarryEyes.Views.Dialogs;
using StarryEyes.Filters.Parsing;
using StarryEyes.Moon.DataModel;
using System.Collections.Generic;
using StarryEyes.Models.Operations;
using StarryEyes.Views.Messaging;
using StarryEyes.ViewModels.WindowParts.Timeline;
using StarryEyes.Views.Helpers;
using StarryEyes.Helpers;

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
                .Select(_ => new StatusViewModel(_, Setting.Accounts.Select(a => a.UserId)))
                .Subscribe(_ => RecentReceived = _.Status);
        }

        private TwitterStatus _recentReceived = null;
        public TwitterStatus RecentReceived
        {
            get { return _recentReceived; }
            set
            {
                _recentReceived = value;
                RaisePropertyChanged(() => RecentReceived);
            }
        }

        #region OpenLinkCommand
        private ListenerCommand<string> _OpenLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get
            {
                if (_OpenLinkCommand == null)
                {
                    _OpenLinkCommand = new ListenerCommand<string>(OpenLink);
                }
                return _OpenLinkCommand;
            }
        }

        public void OpenLink(string parameter)
        {
            var param = RichTextBoxHelper.ResolveInternalUrl(parameter);
            switch (param.Item1)
            {
                case LinkType.User:
                    BrowserHelper.Open("http://twitter.com/" + param.Item2);
                    break;
                case LinkType.Hash:
                    BrowserHelper.Open("http://twitter.com/search/?q=" + param.Item2);
                    break;
                case LinkType.Url:
                    BrowserHelper.Open(param.Item2);
                    break;
            }
        }
        #endregion

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
            var newText = StarryEyes.Models.StatusTextUtil.AutoEscape(PostText);
            if (newText != PostText)
                PostText = newText;
        }

        public int PostTextLength
        {
            get { return StarryEyes.Models.StatusTextUtil.CountText(PostText); }
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
