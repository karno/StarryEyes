using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Threading;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserFollowingViewModel : UserListViewModelBase
    {
        private readonly UserInfoViewModel _parent;

        private List<long> _following;

        public UserFollowingViewModel(UserInfoViewModel parent)
        {
            _parent = parent;
            this.InitCollection();
        }

        private void InitCollection()
        {
            this.IsLoading = true;
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
                this.ReadMore();
            });
            _following = new List<long>();
            info.GetFriendsIdsAll(_parent.User.User.Id)
                .Subscribe(
                    id => _following.Add(id),
                    errorHandler,
                    finishHandler);
        }

        private int _currentPageCount = -1;
        private bool _isDeferLoadEnabled = true;
        protected override void ReadMore()
        {
            if (!_isDeferLoadEnabled || this.IsLoading) return;
            this.IsLoading = true;
            var info = AccountsStore.Accounts
                                    .Shuffle()
                                    .Select(s => s.AuthenticateInfo)
                                    .FirstOrDefault();
            var page = Interlocked.Increment(ref _currentPageCount);
            var ids = _following.Skip(page * 100).Take(100).ToArray();
            if (ids.Length == 0)
            {
                _isDeferLoadEnabled = false;
                IsLoading = false;
                return;
            }
            info.LookupUser(ids)
                .Catch((Exception ex) =>
                {
                    _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                    {
                        CommonButtons = TaskDialogCommonButtons.Close,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "ユーザーの読み込みに失敗しました。",
                        Content = ex.Message,
                        Title = "読み込みエラー"
                    }));
                    BackstageModel.RegisterEvent(new OperationFailedEvent(ex.Message));
                    return Observable.Empty<TwitterUser>();
                })
                .ObserveOn(DispatcherHolder.Dispatcher, DispatcherPriority.Render)
                .Finally(() => this.IsLoading = false)
                .Subscribe(u => Users.Add(new UserResultItemViewModel(u)));
        }
    }
}
