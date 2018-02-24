using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api.Rest;
using Cadena.Data;
using Livet;
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

        public UserInfoViewModel Parent => _parent;

        private readonly ObservableCollection<UserResultItemViewModel> _users =
            new ObservableCollection<UserResultItemViewModel>();

        public ObservableCollection<UserResultItemViewModel> Users => _users;

        private readonly List<long> _userIds = new List<long>();

        public UserListViewModelBase(UserInfoViewModel parent)
        {
            _parent = parent;
            ReadMore();
        }

        protected abstract Task<ICursorResult<IEnumerable<long>>> GetUsersApiImpl(TwitterAccount info, long id,
            long cursor);

        private bool _isLoading;
        private bool _isScrollInBottom;

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public bool IsScrollInBottom
        {
            get { return _isScrollInBottom; }
            set
            {
                if (_isScrollInBottom == value) return;
                _isScrollInBottom = value;
                RaisePropertyChanged();
                if (value)
                {
                    ReadMore();
                }
            }
        }

        private int _currentPageCount = -1;
        private bool _isDeferLoadEnabled = true;

        private void ReadMore()
        {
            if (!_isDeferLoadEnabled || IsLoading) return;
            IsLoading = true;
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
                            MainInstruction =
                                SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
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
                    var result = await ReadMoreIds();
                    IsLoading = false;
                    if (result)
                    {
                        // new users fetched
                        ReadMore();
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
                    var users = await info.CreateAccessor().LookupUserAsync(ids, CancellationToken.None);
                    await DispatcherHelper.UIDispatcher.InvokeAsync(
                        () => users.Result.ForEach(
                            u => Users.Add(new UserResultItemViewModel(u))));
                    IsLoading = false;
                }
                catch (Exception ex)
                {
                    _parent.Parent.Messenger.RaiseSafe(() =>
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                            MainIcon = VistaTaskDialogIcon.Error,
                            MainInstruction =
                                SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
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
                if (_cursor == 0) return false;
                var account = Setting.Accounts.GetRelatedOne(_parent.User.User.Id);
                if (account == null) return false;
                var friends = await GetUsersApiImpl(account, _parent.User.User.Id, _cursor);
                friends.Result.ForEach(_userIds.Add);
                _cursor = friends.NextCursor;
                return true;
            }
            catch (Exception ex)
            {
                _parent.Parent.Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction =
                            SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(UserListName),
                        Content = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close,
                    }));
                return false;
            }
        }
    }
}