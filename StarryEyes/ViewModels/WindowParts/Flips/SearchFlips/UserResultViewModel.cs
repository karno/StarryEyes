using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
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
            this.CompositeDisposable.Add(() =>
            {
                foreach (var vm in _users)
                {
                    vm.Dispose();
                }
                _users.Clear();
            });
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
                    _parent.Messenger.RaiseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction = SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(_query),
                            Content = SearchFlipResources.MsgUserInfoLoadErrorAccountIsNotExist,
                            CommonButtons = TaskDialogCommonButtons.Close,
                        }));
                    return;
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
                    await
                        DispatcherHelper.UIDispatcher.InvokeAsync(
                            () =>
                            twitterUsers.Where(u => Users.All(e => e.User.Id != u.Id)) // add distinct
                                        .ForEach(u => Users.Add(new UserResultItemViewModel(u))));
                }
                catch (Exception ex)
                {
                    _parent.Messenger.RaiseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction = SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(_query),
                            Content = ex.Message,
                            CommonButtons = TaskDialogCommonButtons.Close,
                        }));
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
