using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Models.Hub;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Settings;
using StarryEyes.Models.Operations;
using StarryEyes.Breezy.Authorize;
using System.Reactive.Linq;
using StarryEyes.Models.Store;
using System.Reactive;
using StarryEyes.Models;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class StatusViewModel : ViewModel
    {
        private readonly TabViewModel parent;
        public TabViewModel Parent
        {
            get { return parent; }
        } 

        /// <summary>
        /// Represents status model.
        /// </summary>
        public StatusModel Model { get; private set; }

        /// <summary>
        /// Represents ORIGINAL status. (if this status is retweet, this property represents a status which contains retweeted_original.)
        /// </summary>
        public TwitterStatus OriginalStatus { get { return Model.Status; } }

        /// <summary>
        /// Represents status. (if this status is retweet, this property represents retweeted_original.)
        /// </summary>
        public TwitterStatus Status
        {
            get
            {
                if (Model.Status.RetweetedOriginal != null)
                    return Model.Status.RetweetedOriginal;
                else
                    return Model.Status;
            }
        }

        private long[] _bindingAccounts;
        public IEnumerable<long> BindingAccounts
        {
            get { return _bindingAccounts; }
            set
            {
                _bindingAccounts = value.ToArray();
                // raise property changed
            }
        }

        public StatusViewModel(TwitterStatus status)
            : this(null, status, null) { }

        public StatusViewModel(TabViewModel parent, TwitterStatus status, IEnumerable<long> initialBoundAccounts)
        {
            this.parent = parent;
            this.Model = StatusModel.Get(status);
            this._bindingAccounts = initialBoundAccounts.ToArray();
            this._favoritedUsers = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                Model.FavoritedUsers, _ => new UserViewModel(_), DispatcherHelper.UIDispatcher);
            this._retweetedUsers = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                Model.RetweetedUsers, _ => new UserViewModel(_), DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(_favoritedUsers);
            this.CompositeDisposable.Add(_retweetedUsers);
        }

        private UserViewModel _user;
        public UserViewModel User
        {
            get { return _user ?? (_user = new UserViewModel((Status.RetweetedOriginal ?? Status).User)); }
        }

        private UserViewModel _retweeter;
        public UserViewModel Retweeter
        {
            get { return _retweeter ?? (_retweeter = new UserViewModel(Status.User)); }
        }

        private UserViewModel _recipient;
        public UserViewModel Recipient
        {
            get { return _recipient ?? (_recipient = new UserViewModel(Status.Recipient)); }
        }

        private readonly ReadOnlyDispatcherCollection<UserViewModel> _retweetedUsers;
        public ReadOnlyDispatcherCollection<UserViewModel> RetweetedUsers
        {
            get { return _retweetedUsers; }
        }

        private readonly ReadOnlyDispatcherCollection<UserViewModel> _favoritedUsers;
        public ReadOnlyDispatcherCollection<UserViewModel> FavoritedUsers
        {
            get { return _favoritedUsers; }
        } 

        public bool IsDirectMessage
        {
            get { return Status.StatusType == StatusType.DirectMessage; }
        }

        public bool IsRetweet
        {
            get { return Status.RetweetedOriginal != null; }
        }

        public bool IsFavorited
        {
            get
            {
                return Model.IsFavorited(_bindingAccounts);
            }
        }

        public bool IsRetweeted
        {
            get
            {
                return Model.IsRetweeted(_bindingAccounts);
            }
        }

        public bool CanFavorite
        {
            get
            {
                return !IsDirectMessage;
            }
        }

        public bool CanFavoriteImmediate
        {
            get
            {
                return CanFavorite && (Setting.AllowFavoriteMyself.Value || !IsMyselfStrict);
            }
        }

        public bool CanRetweet
        {
            get
            {
                return !IsDirectMessage;
            }
        }

        public bool CanRetweetImmediate
        {
            get
            {
                return CanRetweet && !IsMyselfStrict;
            }
        }

        public bool CanDelete
        {
            get
            {
                return this.IsDirectMessage || AccountsStore.AccountIds.Contains(OriginalStatus.User.Id);
            }
        }

        public bool IsMyself
        {
            get { return AccountsStore.AccountIds.Contains(Status.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get
            {
                return this._bindingAccounts.Length == 1 && this._bindingAccounts[0] == Status.User.Id;
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value && Parent != null)
                {
                    _isSelected = value;
                    RaisePropertyChanged(() => IsSelected);
                    Parent.OnSelectionUpdated();
                }
            }
        }

        #region Execution commands

        private void Favorite(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected = null;
            Action<AuthenticateInfo> onFail = null;
            if (add)
            {
                expected = a => Model.AddFavoritedUser(a.Id);
                onFail = a => Model.RemoveFavoritedUser(a.Id);
            }
            else
            {
                expected = a => Model.RemoveFavoritedUser(a.Id);
                onFail = a => Model.AddFavoritedUser(a.Id);
            }

            infos.ToObservable()
                .Do(expected)
                .SelectMany(a => new FavoriteOperation(a, this.Status, add)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        onFail(a);
                        return Observable.Empty<TwitterStatus>();
                    }))
                .Subscribe();
        }

        private void Retweet(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected = null;
            Action<AuthenticateInfo> onFail = null;
            if (add)
            {
                expected = a => Model.AddRetweetedUser(a.Id);
                onFail = a => Model.RemoveRetweetedUser(a.Id);
            }
            else
            {
                expected = a => Model.RemoveRetweetedUser(a.Id);
                onFail = a => Model.AddRetweetedUser(a.Id);
            }
            infos.ToObservable()
                .Do(expected)
                .Select(a =>
                    new RetweetOperation(a, this.Status, add)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        onFail(a);
                        return Observable.Empty<TwitterStatus>();
                    }))
                .Subscribe();
        }

        public void ToggleFavoriteImmediate()
        {
            Favorite(GetImmediateAccounts(), !IsFavorited);
        }

        public void ToggleRetweetImmediate()
        {
            Retweet(GetImmediateAccounts(), !IsRetweeted);
        }

        public void FavoriteAndRetweetImmediate()
        {
            var accounts = GetImmediateAccounts()
                .ToObservable()
                .Publish();
            if (!IsFavorited)
                accounts.Do(a => Model.AddFavoritedUser(a.Id))
                    .SelectMany(a => new FavoriteOperation(a, this.Status, true)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        Model.RemoveFavoritedUser(a.Id);
                        return Observable.Empty<TwitterStatus>();
                    }))
                    .Subscribe();
            if (!IsRetweeted)
                accounts.Do(a => Model.AddRetweetedUser(a.Id))
                    .SelectMany(a => new RetweetOperation(a, this.Status, true)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        Model.RemoveRetweetedUser(a.Id);
                        return Observable.Empty<TwitterStatus>();
                    }))
                    .Subscribe();
            accounts.Connect();
        }

        private IEnumerable<AuthenticateInfo> GetImmediateAccounts()
        {
            return AccountsStore.Accounts
                .Where(a => _bindingAccounts.Contains(a.UserId))
                .Select(a => a.AuthenticateInfo);
        }

        public void ToggleFavorite()
        {
            var favoriteds = AccountsStore.Accounts
                .Where(a => Model.IsFavorited(a.UserId))
                .Select(a => a.AuthenticateInfo)
                .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(AccountSelectionAction.Favorite,
                this.Status,
                favoriteds,
                infos =>
                {
                    var adds = infos.Except(favoriteds);
                    Favorite(adds, true);
                    var rmvs = favoriteds.Except(infos);
                    Favorite(rmvs, false);
                });
        }

        public void ToggleRetweet()
        {
            var retweeteds = AccountsStore.Accounts
                .Where(a => Model.IsRetweeted(a.UserId))
                .Select(a => a.AuthenticateInfo)
                .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(AccountSelectionAction.Retweet,
                this.Status,
                retweeteds,
                infos =>
                {
                    var adds = infos.Except(retweeteds);
                    Retweet(adds, true);
                    var rmvs = retweeteds.Except(infos);
                    Favorite(rmvs, false);
                });
        }

        public void Reply()
        {
            InputAreaModel.SetText(infos: Model.GetSuitableReplyAccount(),
                body: "@" + this.User.ScreenName, inReplyTo: this.Status);
        }

        public void DirectMessage()
        {
            InputAreaModel.SetDirectMessage(Model.GetSuitableReplyAccount(), this.Status.User);
        }

        public void Delete()
        {
            AuthenticateInfo info = null;
            if (IsDirectMessage)
            {
                var ids = new[] { this.Status.User.Id, this.Status.Recipient.Id };
                info = ids
                    .Select(AccountsStore.GetAccountSetting)
                    .Where(_ => _ != null)
                    .Select(_ => _.AuthenticateInfo)
                    .FirstOrDefault();
            }
            else
            {
                var ai = AccountsStore.GetAccountSetting(this.OriginalStatus.User.Id);
                if (ai != null)
                {
                    info = ai.AuthenticateInfo;
                }
            }
            if (info != null)
            {
                new DeleteOperation(info, this.OriginalStatus)
                .Run()
                .Subscribe(_ => StatusStore.Remove(_.Id),
                ex => AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Error,
                    "ERR_DELETE_MSG_" + this.Status.Id, "ステータスを削除できませんでした。", ex.Message,
                    "再試行", Delete)));
            }
        }

        #endregion
    }
}
