using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public abstract class UserListViewModelBase : ViewModel
    {
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

        private List<long> _userIds;

        public UserListViewModelBase(UserInfoViewModel parent)
        {
            this._parent = parent;
            this.InitCollection();
        }

        private async void InitCollection()
        {

            this.IsLoading = true;
            var account =
                Setting.Accounts.Collection.FirstOrDefault(a => a.RelationData.IsFollowing(this._parent.User.User.Id)) ??
                Setting.Accounts.GetRandomOne();
            if (account == null)
            {
                return;
            }
            long cursor = -1;
            try
            {
                while (cursor != 0)
                {
                    _userIds = new List<long>();
                    var friends = await this.GetUsersApiImpl(account, _parent.User.User.Id, cursor);
                    friends.Result.ForEach(_userIds.Add);
                    cursor = friends.NextCursor;
                }
                IsLoading = false;
                this.ReadMore();
            }
            catch (Exception ex)
            {
                this._parent.Parent.Messenger.Raise(
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        CommonButtons = TaskDialogCommonButtons.Close,
                        MainIcon = VistaTaskDialogIcon.Error,
                        MainInstruction = "ユーザー情報を受信できませんでした。",
                        Content = ex.Message,
                        Title = "ユーザー受信エラー",
                    }));
            }
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
        private async void ReadMore()
        {
            if (!_isDeferLoadEnabled || this.IsLoading) return;
            this.IsLoading = true;
            var info = Setting.Accounts.GetRandomOne();
            if (info == null)
            {
                _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
            var ids = _userIds.Skip(page * 100).Take(100).ToArray();
            if (ids.Length == 0)
            {
                _isDeferLoadEnabled = false;
                IsLoading = false;
                return;
            }
            try
            {
                (await info.LookupUser(ids)).ForEach(u => Users.Add(new UserResultItemViewModel(u)));
                this.IsLoading = false;
            }
            catch (Exception ex)
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
            }
        }
    }
}