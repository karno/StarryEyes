using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines.Statuses;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserResultViewModel : ViewModel
    {
        private readonly SearchFlipViewModel _parent;

        private readonly string _query;
        public string Query
        {
            get { return this._query; }
        }

        private readonly ObservableCollection<UserResultItemViewModel> _users = new ObservableCollection<UserResultItemViewModel>();
        public ObservableCollection<UserResultItemViewModel> Users
        {
            get { return _users; }
        }

        public SearchFlipViewModel Parent
        {
            get { return this._parent; }
        }

        public bool IsLoading
        {
            get { return this._isLoading; }
            set
            {
                this._isLoading = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _isDeferLoadEnabled = true;
        private bool _isLoading;
        private int _currentPageCount = -1;
        private bool _isScrollInBottom;

        public UserResultViewModel(SearchFlipViewModel parent, string query)
        {
            this._parent = parent;
            this._query = query;
            LoadMore();
        }

        public bool IsScrollInBottom
        {
            get { return this._isScrollInBottom; }
            set
            {
                if (this._isScrollInBottom == value) return;
                this._isScrollInBottom = value;
                if (value)
                {
                    this.LoadMore();
                }
            }
        }

        public void Close()
        {
            this.Parent.RewindStack();
        }

        public void LoadMore()
        {
            if (!_isDeferLoadEnabled || this.IsLoading) return;
            this.IsLoading = true;
            Task.Run(async () =>
            {
                var account = Setting.Accounts.GetRandomOne();
                if (account == null)
                {
                    _parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                    {
                        CommonButtons = TaskDialogCommonButtons.Close,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "ユーザーの読み込みに失敗しました。",
                        Content = "アカウントが登録されていません。",
                        Title = "読み込みエラー"
                    }));
                    BackstageModel.RegisterEvent(new OperationFailedEvent("アカウントが登録されていません。"));
                }
                var page = Interlocked.Increment(ref _currentPageCount);
                try
                {
                    var result = await account.SearchUserAsync(this.Query, count: 100, page: page);
                    var twitterUsers = result as TwitterUser[] ?? result.ToArray();
                    if (!twitterUsers.Any())
                    {
                        _isDeferLoadEnabled = false;
                        return;
                    }
                    await DispatcherHelper.UIDispatcher.InvokeAsync(() => twitterUsers.ForEach(u => Users.Add(new UserResultItemViewModel(u))));
                }
                catch (Exception ex)
                {
                    _parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                    {
                        CommonButtons = TaskDialogCommonButtons.Close,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "ユーザーの読み込みに失敗しました。",
                        Content = ex.Message,
                        Title = "読み込みエラー"
                    }));
                    BackstageModel.RegisterEvent(new OperationFailedEvent(ex.Message));
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }
    }

    public class UserResultItemViewModel : UserViewModel
    {
        public UserResultItemViewModel(TwitterUser user)
            : base(user)
        {
        }

        public void Select()
        {
            SearchFlipModel.RequestSearch(User.ScreenName, SearchMode.UserScreenName);
        }
    }
}
