using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFriendsViewModel : ViewModel
    {
        private readonly UserInfoViewModel _parent;

        private List<long> _following;
        private List<long> _followers;

        private readonly ObservableCollection<UserResultItemViewModel> _users = new ObservableCollection<UserResultItemViewModel>();
        private RelationKind _relationKind;

        public ObservableCollection<UserResultItemViewModel> Users
        {
            get { return this._users; }
        }

        public UserFriendsViewModel(UserInfoViewModel parent)
        {
            _parent = parent;
            this.InitCollection();
        }

        public RelationKind RelationKind
        {
            get { return this._relationKind; }
            set
            {
                if (this._relationKind == value) return;
                this._relationKind = value;
                this.InitCollection();
            }
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

        private void InitCollection()
        {
            this.IsLoading = true;
            _users.Clear();
            var info = AccountsStore.Accounts
                                    .Shuffle()
                                    .Select(s => s.AuthenticateInfo)
                                    .FirstOrDefault();
            var errorHandler = new Action<Exception>(
                ex => this._parent.Parent.Messenger.Raise(
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        CommonButtons = TaskDialogCommonButtons.Close,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "ユーザー情報を受信できませんでした。",
                        Content = ex.Message,
                        Title = "ユーザー受信エラー",
                    })));
            var finishHandler = new Action(() =>
            {
                IsLoading = false;
                this.LoadMore();
            });
            switch (RelationKind)
            {
                case RelationKind.Following:
                    _following = new List<long>();
                    info.GetFriendsIdsAll(_parent.User.User.Id)
                        .Subscribe(
                            id => _following.Add(id),
                            errorHandler,
                            finishHandler);
                    break;
                case RelationKind.Followers:
                    _followers = new List<long>();
                    info.GetFollowerIdsAll(_parent.User.User.Id)
                        .Subscribe(
                            id => _followers.Add(id),
                            errorHandler,
                            finishHandler);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int _currentPageCount = -1;
        private bool _isDeferLoadEnabled = true;
        private bool _isLoading;
        public void LoadMore()
        {
            if (!_isDeferLoadEnabled || this.IsLoading) return;
            this.IsLoading = true;
            var info = AccountsStore.Accounts
                                    .Shuffle()
                                    .Select(s => s.AuthenticateInfo)
                                    .FirstOrDefault();
            var page = Interlocked.Increment(ref _currentPageCount);
            var target = RelationKind == RelationKind.Following ? _following : _followers;
            var ids = target.Skip(page * 100).Take(100).ToArray();
            if (ids.Length == 0)
            {
                _isDeferLoadEnabled = false;
                return;
            }
            info.LookupUser(ids)
                .ObserveOn(DispatcherHolder.Dispatcher)
                .Finally(() => this.IsLoading = false)
                .Subscribe(u => Users.Add(new UserResultItemViewModel(u)));
        }
    }

    public enum RelationKind
    {
        Following,
        Followers,
    }
}
