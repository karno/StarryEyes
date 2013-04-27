using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using Livet;
using Livet.Commands;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Util;
using StarryEyes.Models;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Views.Messaging;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserInfoViewModel : ViewModel
    {
        private readonly SearchFlipViewModel _parent;
        private readonly string _screenName;
        private UserStatusesViewModel _statuses;
        private UserFavoritesViewModel _favorites;
        private UserFriendsViewModel _friends;
        private bool _communicating = true;
        private UserViewModel _user;

        private UserDisplayKind _displayKind = UserDisplayKind.Statuses;
        public UserDisplayKind DisplayKind
        {

            get { return _displayKind; }
            set
            {
                if (_displayKind == value) return;
                this._displayKind = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => IsVisibleStatuses);
                this.RaisePropertyChanged(() => IsVisibleFriends);
                this.RaisePropertyChanged(() => IsVisibleFavorites);
            }
        }

        public SearchFlipViewModel Parent
        {
            get { return this._parent; }
        }

        public string ScreenName
        {
            get { return this._screenName; }
        }

        public bool Communicating
        {
            get { return this._communicating; }
            set
            {
                this._communicating = value;
                this.RaisePropertyChanged();
            }
        }

        public UserViewModel User
        {
            get { return this._user; }
            set
            {
                this._user = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => IsUserAvailable);
            }
        }

        public bool IsUserAvailable
        {
            get { return User != null; }
        }

        public UserStatusesViewModel Statuses
        {
            get { return this._statuses; }
        }

        public UserFavoritesViewModel Favorites
        {
            get { return this._favorites; }
        }

        public UserFriendsViewModel Friends
        {
            get { return this._friends; }
        }

        public bool IsVisibleStatuses
        {
            get { return DisplayKind == UserDisplayKind.Statuses; }
        }

        public bool IsVisibleFavorites
        {
            get { return DisplayKind == UserDisplayKind.Favorites; }
        }

        public bool IsVisibleFriends
        {
            get { return DisplayKind == UserDisplayKind.Following || DisplayKind == UserDisplayKind.Followers; }
        }

        public UserInfoViewModel(SearchFlipViewModel parent, string screenName)
        {
            this._parent = parent;
            this._screenName = screenName;
            StoreHelper.GetUser(screenName)
                       .Finally(() => Communicating = false)
                       .Subscribe(
                           user =>
                           {
                               User = new UserViewModel(user);
                               this._statuses = new UserStatusesViewModel(this);
                               this.RaisePropertyChanged(() => Statuses);
                               this._favorites = new UserFavoritesViewModel(this);
                               this.RaisePropertyChanged(() => Favorites);
                               this._friends = new UserFriendsViewModel(this);
                               this.RaisePropertyChanged(() => Friends);
                           },
                           ex =>
                           {
                               parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                               {
                                   Title = "ユーザー表示エラー",
                                   MainIcon = VistaTaskDialogIcon.Error,
                                   MainInstruction = "ユーザーを表示できません。",
                                   Content = ex.Message,
                                   CommonButtons = TaskDialogCommonButtons.Close
                               }));
                               User = null;
                           });
        }

        public void Close()
        {
            Parent.RewindStack();
        }

        public void ShowStatuses()
        {
            DisplayKind = UserDisplayKind.Statuses;
        }

        public void ShowFavorites()
        {
            DisplayKind = UserDisplayKind.Favorites;
        }

        public void ShowFollowing()
        {
            DisplayKind = UserDisplayKind.Following;
            Friends.RelationKind = RelationKind.Following;
        }

        public void ShowFollowers()
        {
            DisplayKind = UserDisplayKind.Followers;
            Friends.RelationKind = RelationKind.Followers;
        }

        #region Text selection control

        private string _selectedText;
        public string SelectedText
        {
            get { return this._selectedText ?? String.Empty; }
            set
            {
                this._selectedText = value;
                this.RaisePropertyChanged();
            }
        }

        public void CopyText()
        {
            try
            {
                Clipboard.SetText(SelectedText);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        public void SetTextToInputBox()
        {
            InputAreaModel.SetText(body: SelectedText);
        }

        public void FindOnKrile()
        {
            SearchFlipModel.RequestSearch(SelectedText, SearchMode.Local);
        }

        public void FindOnTwitter()
        {
            SearchFlipModel.RequestSearch(SelectedText, SearchMode.Web);
        }

        private const string GoogleUrl = @"http://www.google.com/search?q={0}";
        public void FindOnGoogle()
        {
            var encoded = HttpUtility.UrlEncode(SelectedText);
            var url = String.Format(GoogleUrl, encoded);
            BrowserHelper.Open(url);
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
            var param = TextBlockStylizer.ResolveInternalUrl(parameter);
            switch (param.Item1)
            {
                case LinkType.User:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.UserScreenName);
                    break;
                case LinkType.Hash:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.Web);
                    break;
                case LinkType.Url:
                    BrowserHelper.Open(param.Item2);
                    break;
            }
        }

        #endregion
    }

    public enum UserDisplayKind
    {
        Statuses,
        Favorites,
        Following,
        Followers,
    }

    public class RelationControllerViewModel : ViewModel
    {
        private readonly UserInfoViewModel _parent;
        private readonly AuthenticateInfo _source;
        private readonly TwitterUser _target;
        private bool _isCommunicating;
        private bool _enabled;
        private bool _isFollowing;
        private bool _isFollowedBack;
        private bool _isBlocking;

        public string SourceUserScreenName
        {
            get { return _source.UnreliableScreenName; }
        }

        public Uri SourceUserProfileImage
        {
            get { return _source.UnreliableProfileImageUri; }
        }

        public bool IsCommunicating
        {
            get { return this._isCommunicating; }
            set
            {
                this._isCommunicating = value;
                this.RaisePropertyChanged();
            }
        }

        public bool Enabled
        {
            get { return this._enabled; }
            set
            {
                this._enabled = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsFollowing
        {
            get { return this._isFollowing; }
            set
            {
                this._isFollowing = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsFollowedBack
        {
            get { return this._isFollowedBack; }
            set
            {
                this._isFollowedBack = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsBlocking
        {
            get { return this._isBlocking; }
            set
            {
                this._isBlocking = value;
                this.RaisePropertyChanged();
            }
        }

        public RelationControllerViewModel(UserInfoViewModel parent, AuthenticateInfo source, TwitterUser target)
        {
            _parent = parent;
            _source = source;
            _target = target;
            var rds = source.GetRelationData();
            IsFollowing = rds.IsFollowing(target.Id);
            IsFollowedBack = rds.IsFollowedBy(target.Id);
            IsBlocking = rds.IsBlocking(target.Id);
            source.GetFriendship(source.Id, target_id: target.Id)
                  .Subscribe(
                      r =>
                      {
                          // ReSharper disable InvertIf
                          if (IsFollowing != r.relationship.source.following)
                          {
                              IsFollowing = r.relationship.source.following;
                              if (r.relationship.source.following)
                              {
                                  rds.AddFollowing(target.Id);
                              }
                              else
                              {
                                  rds.RemoveFollowing(target.Id);
                              }
                          }
                          if (IsFollowedBack != r.relationship.source.followed_by)
                          {
                              IsFollowedBack = r.relationship.source.followed_by;
                              if (r.relationship.source.followed_by)
                              {
                                  rds.AddFollower(target.Id);
                              }
                              else
                              {
                                  rds.RemoveFollower(target.Id);
                              }
                          }
                          if (IsBlocking != r.relationship.source.blocking)
                          {
                              IsBlocking = r.relationship.source.blocking;
                              if (r.relationship.source.blocking)
                              {
                                  rds.AddBlocking(target.Id);
                              }
                              else
                              {
                                  rds.RemoveBlocking(target.Id);
                              }
                          }
                          // ReSharper restore InvertIf
                      },
                      ex =>
                      {
                          Enabled = false;
                      });
        }

        public void Follow()
        {
            this.DispatchAction(
                () =>
                this._source.CreateFriendship(_target.Id)
                    .Select(_ => new Unit()),
                () => IsFollowing = true,
                ex => _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "フォローできませんでした。",
                    Content = ex.Message,
                    Title = "フォロー エラー",
                })));
        }

        public void Remove()
        {
            this.DispatchAction(
                () =>
                this._source.DestroyFriendship(_target.Id)
                    .Select(_ => new Unit()),
                () => IsFollowing = false,
                ex => _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "フォロー解除できませんでした。",
                    Content = ex.Message,
                    Title = "アンフォロー エラー",
                })));
        }

        public void Block()
        {
            this.DispatchAction(
                () =>
                this._source.CreateBlock(_target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    IsFollowing = false;
                    IsBlocking = true;
                },
                ex => _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "ブロックできませんでした。",
                    Content = ex.Message,
                    Title = "ブロック エラー",
                })));
        }

        public void Unblock()
        {
            this.DispatchAction(
                () =>
                this._source.DestroyBlock(_target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    IsBlocking = false;
                },
                ex => _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "ブロックを解除できませんでした。",
                    Content = ex.Message,
                    Title = "アンブロック エラー",
                })));
        }

        public void ReportForSpam()
        {
            this.DispatchAction(
                () =>
                this._source.ReportSpam(_target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    IsFollowing = false;
                    IsBlocking = true;
                },
                ex => _parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "スパム報告できませんでした。",
                    Content = ex.Message,
                    Title = "スパム報告 エラー",
                })));
        }

        private void DispatchAction(Func<IObservable<Unit>> work, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            work().Retry(3)
                  .Finally(() => IsCommunicating = false)
                  .Subscribe(_ => { },
                             failed,
                             succeeded);
        }
    }
}
