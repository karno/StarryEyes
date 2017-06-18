using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Accounting;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public abstract class UserListViewModelBase : ViewModel
    {
        protected abstract string UserListName { get; }

        private readonly UserInfoViewModel _parent;

        public UserInfoViewModel Parent
        {
            get { return this._parent; }
        }

        private readonly ObservableCollection<UserResultItemViewModel> _users = new ObservableCollection<UserResultItemViewModel>();

        public ObservableCollection<UserResultItemViewModel> Users
        {
            get { return this._users; }
        }

        private readonly List<long> _userIds = new List<long>();

        public UserListViewModelBase(UserInfoViewModel parent)
        {
            this._parent = parent;
            this.ReadMore();
        }

        protected abstract Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id, long cursor);

        private bool _isLoading;
        private bool _isScrollInBottom;

        public bool IsLoading
        {
            get { return this._isLoading; }
            set
            {
                this._isLoading = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsScrollInBottom
        {
            get { return this._isScrollInBottom; }
            set
            {
                if (this._isScrollInBottom == value) return;
                this._isScrollInBottom = value;
                this.RaisePropertyChanged();
                if (value)
                {
                    this.ReadMore();
                }
            }
        }

        private int _currentPageCount = -1;
        private bool _isDeferLoadEnabled = true;
        private void ReadMore()
        {
            if (!_isDeferLoadEnabled || this.IsLoading) return;
            this.IsLoading = true;
            Task.Run(async () =>
            {
                var info = Setting.Accounts.GetRandomOne();
                if (info == null)
                {
                    _parent.Parent.Messenger.RaiseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction = SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
                            Content = SearchFlipResources.MsgUserInfoLoadErrorAccountIsNotExist,
                            CommonButtons = TaskDialogCommonButtons.Close,
                        }));
                    return;
                }
                var page = Interlocked.Increment(ref _currentPageCount);
                var ids = _userIds.Skip(page * 100).Take(100).ToArray();
                if (ids.Length == 0)
                {
                    // backward page count
                    Interlocked.Decrement(ref _currentPageCount);
                    var result = await this.ReadMoreIds();
                    IsLoading = false;
                    if (result)
                    {
                        // new users fetched
                        this.ReadMore();
                    }
                    else
                    {
                        // end of user list
                        _isDeferLoadEnabled = false;
                    }
                    return;
                }
                try
                {
                    var users = await info.LookupUserAsync(ids);
                    await DispatcherHelper.UIDispatcher.InvokeAsync(
                        () => users.ForEach(u => Users.Add(new UserResultItemViewModel(u))));
                    this.IsLoading = false;
                }
                catch (Exception ex)
                {
                    _parent.Parent.Messenger.RaiseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction = SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
                            Content = ex.Message,
                            CommonButtons = TaskDialogCommonButtons.Close,
                        }));
                }
            });
        }

        private long _cursor = -1;
        private async Task<bool> ReadMoreIds()
        {
            try
            {
                if (this._cursor == 0) return false;
                var account = Setting.Accounts.GetRelatedOne(this._parent.User.User.Id);
                if (account == null) return false;
                var friends = await this.GetUsersApiImpl(account, _parent.User.User.Id, this._cursor);
                friends.Result.ForEach(_userIds.Add);
                this._cursor = friends.NextCursor;
                return true;
            }
            catch (Exception ex)
            {
                _parent.Parent.Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
                        Content = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close,
                    }));
                return false;
            }
        }
    }
}