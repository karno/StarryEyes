using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Livet;
using Livet.Commands;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
using StarryEyes.Views.Helpers;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class StatusViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollection<UserViewModel> _favoritedUsers;
        private readonly ReadOnlyDispatcherCollection<UserViewModel> _retweetedUsers;
        private readonly TabViewModel _parent;
        private long[] _bindingAccounts;
        private bool _isSelected;
        private UserViewModel _recipient;
        private UserViewModel _retweeter;
        private UserViewModel _user;

        public StatusViewModel(TwitterStatus status)
            : this(null, status, null)
        {
        }

        public StatusViewModel(TabViewModel parent, TwitterStatus status,
                               IEnumerable<long> initialBoundAccounts)
        {
            _parent = parent;
            Model = StatusModel.Get(status);
            _bindingAccounts = initialBoundAccounts.Guard().ToArray();
            _favoritedUsers = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                Model.FavoritedUsers, _ => new UserViewModel(_), DispatcherHelper.UIDispatcher);
            _favoritedUsers.CollectionChanged += (sender, e) =>
            {
                RaisePropertyChanged(() => IsFavoritedUserExists);
                RaisePropertyChanged(() => FavoriteCount);
            };
            _retweetedUsers = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                Model.RetweetedUsers, _ => new UserViewModel(_), DispatcherHelper.UIDispatcher);
            _retweetedUsers.CollectionChanged += (sender, e) =>
            {
                RaisePropertyChanged(() => IsRetweetedUserExists);
                RaisePropertyChanged(() => RetweetCount);
            };
            CompositeDisposable.Add(_favoritedUsers);
            CompositeDisposable.Add(_retweetedUsers);
        }

        public TabViewModel Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///     Represents status model.
        /// </summary>
        public StatusModel Model { get; private set; }

        /// <summary>
        ///     Represents ORIGINAL status. (if this status is retweet, this property represents a status which contains retweeted_original.)
        /// </summary>
        public TwitterStatus OriginalStatus
        {
            get { return Model.Status; }
        }

        /// <summary>
        ///     Represents status. (if this status is retweet, this property represents retweeted_original.)
        /// </summary>
        public TwitterStatus Status
        {
            get
            {
                if (Model.Status.RetweetedOriginal != null)
                    return Model.Status.RetweetedOriginal;
                return Model.Status;
            }
        }

        public IEnumerable<long> BindingAccounts
        {
            get { return _bindingAccounts; }
            set
            {
                _bindingAccounts = value.ToArray();
                // raise property changed
            }
        }

        public UserViewModel User
        {
            get { return _user ?? (_user = new UserViewModel((Status.RetweetedOriginal ?? Status).User)); }
        }

        public UserViewModel Retweeter
        {
            get { return _retweeter ?? (_retweeter = new UserViewModel(OriginalStatus.User)); }
        }

        public UserViewModel Recipient
        {
            get { return _recipient ?? (_recipient = new UserViewModel(Status.Recipient)); }
        }

        public bool IsRetweetedUserExists
        {
            get { return _retweetedUsers.Count > 0; }
        }

        public int RetweetCount
        {
            get { return RetweetedUsers.Count; }
        }

        public ReadOnlyDispatcherCollection<UserViewModel> RetweetedUsers
        {
            get { return _retweetedUsers; }
        }

        public bool IsFavoritedUserExists
        {
            get { return _favoritedUsers.Count > 0; }
        }

        public int FavoriteCount
        {
            get { return FavoritedUsers.Count; }
        }

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
            get { return OriginalStatus.RetweetedOriginal != null; }
        }

        public bool IsFavorited
        {
            get { return Model.IsFavorited(_bindingAccounts); }
        }

        public bool IsRetweeted
        {
            get { return Model.IsRetweeted(_bindingAccounts); }
        }

        public bool CanFavorite
        {
            get { return !IsDirectMessage; }
        }

        public bool CanFavoriteImmediate
        {
            get { return CanFavorite && (Setting.AllowFavoriteMyself.Value || !IsMyselfStrict); }
        }

        public bool CanRetweet
        {
            get { return !IsDirectMessage; }
        }

        public bool CanRetweetImmediate
        {
            get { return CanRetweet && !IsMyselfStrict; }
        }

        public bool CanDelete
        {
            get { return IsDirectMessage || AccountsStore.AccountIds.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyself
        {
            get { return AccountsStore.AccountIds.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get { return _bindingAccounts.Length == 1 && _bindingAccounts[0] == Status.User.Id; }
        }

        public bool IsInReplyToMe
        {
            get
            {
                return FilterSystemUtil.InReplyToUsers(Status)
                                       .Any(AccountsStore.AccountIds.Contains);
            }
        }

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

        public bool IsSourceVisible
        {
            get { return Status.StatusType != StatusType.DirectMessage; }
        }

        public bool IsSourceIsLink
        {
            get { return Status.Source != null && Status.Source.Contains("<a href"); }
        }

        public string SourceText
        {
            get
            {
                if (IsSourceIsLink)
                {
                    var start = Status.Source.IndexOf(">", StringComparison.Ordinal);
                    var end = Status.Source.IndexOf("<", start + 1, StringComparison.Ordinal);
                    return Status.Source.Substring(start + 1, end - start - 1);
                }
                return Status.Source;
            }
        }

        public void OpenSourceLink()
        {
            if (!IsSourceIsLink) return;
            var start = Status.Source.IndexOf("\"", StringComparison.Ordinal);
            var end = Status.Source.IndexOf("\"", start + 1, StringComparison.Ordinal);
            var url = Status.Source.Substring(start + 1, end - start - 1);
            BrowserHelper.Open(url);
        }

        #region Execution commands

        private void Favorite(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected;
            Action<AuthenticateInfo> onFail;
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
                 .SelectMany(a => new FavoriteOperation(a, Status, add)
                                      .Run()
                                      .Catch((Exception ex) =>
                                      {
                                          onFail(a);
                                          BackpanelModel.RegisterEvent(
                                              new OperationFailedEvent((add ? "" : "un") + "favorite failed: " +
                                                                       a.UnreliableScreenName + " -> " +
                                                                       Status.User.ScreenName + " :" +
                                                                       ex.Message));
                                          return Observable.Empty<TwitterStatus>();
                                      }))
                 .Subscribe();
        }

        private void Retweet(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected;
            Action<AuthenticateInfo> onFail;
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
                 .SelectMany(a => new RetweetOperation(a, Status, add)
                                      .Run()
                                      .Catch((Exception ex) =>
                                      {
                                          onFail(a);
                                          BackpanelModel.RegisterEvent(
                                              new OperationFailedEvent((add ? "" : "un") + "retweet failed: " +
                                                                       a.UnreliableScreenName + " -> " +
                                                                       Status.User.ScreenName + " :" +
                                                                       ex.Message));
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
            IConnectableObservable<AuthenticateInfo> accounts = GetImmediateAccounts()
                .ToObservable()
                .Publish();
            if (!IsFavorited)
                accounts.Do(a => Model.AddFavoritedUser(a.Id))
                        .SelectMany(a => new FavoriteOperation(a, Status, true)
                                             .Run()
                                             .Catch((Exception ex) =>
                                             {
                                                 Model.RemoveFavoritedUser(a.Id);
                                                 return Observable.Empty<TwitterStatus>();
                                             }))
                        .Subscribe();
            if (!IsRetweeted)
                accounts.Do(a => Model.AddRetweetedUser(a.Id))
                        .SelectMany(a => new RetweetOperation(a, Status, true)
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

        public void ToggleSelect()
        {
            IsSelected = !IsSelected;
        }

        public void ToggleFavorite()
        {
            AuthenticateInfo[] favoriteds = AccountsStore.Accounts
                                                         .Where(a => Model.IsFavorited(a.UserId))
                                                         .Select(a => a.AuthenticateInfo)
                                                         .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(AccountSelectionAction.Favorite,
                                                       Status,
                                                       favoriteds,
                                                       infos =>
                                                       {
                                                           var authenticateInfos = infos as AuthenticateInfo[] ?? infos.ToArray();
                                                           IEnumerable<AuthenticateInfo> adds = authenticateInfos.Except(favoriteds);
                                                           IEnumerable<AuthenticateInfo> rmvs = favoriteds.Except(authenticateInfos);
                                                           Favorite(adds, true);
                                                           Favorite(rmvs, false);
                                                       });
        }

        public void ToggleRetweet()
        {
            AuthenticateInfo[] retweeteds = AccountsStore.Accounts
                                                         .Where(a => Model.IsRetweeted(a.UserId))
                                                         .Select(a => a.AuthenticateInfo)
                                                         .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(AccountSelectionAction.Retweet,
                                                       Status,
                                                       retweeteds,
                                                       infos =>
                                                       {
                                                           var authenticateInfos = infos as AuthenticateInfo[] ?? infos.ToArray();
                                                           IEnumerable<AuthenticateInfo> adds = authenticateInfos.Except(retweeteds);
                                                           IEnumerable<AuthenticateInfo> rmvs = retweeteds.Except(authenticateInfos);
                                                           Retweet(adds, true);
                                                           Retweet(rmvs, false);
                                                       });
        }

        public void Reply()
        {
            InputAreaModel.SetText(Model.GetSuitableReplyAccount(), "@" + User.ScreenName, inReplyTo: Status);
        }

        public void DirectMessage()
        {
            InputAreaModel.SetDirectMessage(Model.GetSuitableReplyAccount(), Status.User);
        }

        public void Delete()
        {
            AuthenticateInfo info = null;
            if (IsDirectMessage)
            {
                var ids = new[] { Status.User.Id, Status.Recipient.Id };
                info = ids
                    .Select(AccountsStore.GetAccountSetting)
                    .Where(_ => _ != null)
                    .Select(_ => _.AuthenticateInfo)
                    .FirstOrDefault();
            }
            else
            {
                AccountSetting ai = AccountsStore.GetAccountSetting(OriginalStatus.User.Id);
                if (ai != null)
                {
                    info = ai.AuthenticateInfo;
                }
            }
            if (info != null)
            {
                new DeleteOperation(info, OriginalStatus)
                    .Run()
                    .Subscribe(_ => StatusStore.Remove(_.Id),
                               ex => AppInformationHub.PublishInformation(new AppInformation(AppInformationKind.Error,
                                                                                             "ERR_DELETE_MSG_" +
                                                                                             Status.Id,
                                                                                             "ステータスを削除できませんでした。",
                                                                                             ex.Message,
                                                                                             "再試行", Delete)));
            }
        }

        #endregion

        #region OpenLinkCommand

        private ListenerCommand<string> _openLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get { return _openLinkCommand ?? (_openLinkCommand = new ListenerCommand<string>(OpenLink)); }
        }

        public void OpenLink(string parameter)
        {
            Tuple<LinkType, string> param = RichTextBoxHelper.ResolveInternalUrl(parameter);
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
    }
}