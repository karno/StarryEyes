using System;
using System.Reactive;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserInfoViewModel : ViewModel
    {
        private DisplayMode _currentDisplayMode = DisplayMode.None;
        private readonly SearchFlipViewModel _parent;
        private readonly string _screenName;
        private readonly UserStatusesViewModel _statuses;
        private readonly UserFriendsViewModel _friends;
        private bool _communicating = true;
        private UserViewModel _user;

        private DisplayMode CurrentDisplayMode
        {
            get { return this._currentDisplayMode; }
            set
            {
                this._currentDisplayMode = value;
                this.RaisePropertyChanged(() => IsVisibleStatuses);
                this.RaisePropertyChanged(() => IsVisibleFriends);
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

        public UserFriendsViewModel Friends
        {
            get { return this._friends; }
        }

        public bool IsVisibleStatuses
        {
            get { return CurrentDisplayMode == DisplayMode.Statuses; }
        }

        public bool IsVisibleFriends
        {
            get { return CurrentDisplayMode == DisplayMode.Friends; }
        }

        public UserInfoViewModel(SearchFlipViewModel parent, string screenName)
        {
            this._parent = parent;
            this._screenName = screenName;
            this._statuses = new UserStatusesViewModel(this);
            this._friends = new UserFriendsViewModel(this);
            StoreHelper.GetUser(screenName)
                       .Finally(() => Communicating = false)
                       .Subscribe(user => User = new UserViewModel(user),
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

        public void ShowStatuses()
        {
            CurrentDisplayMode = DisplayMode.Statuses;
        }

        public void ShowFollowings()
        {
            CurrentDisplayMode = DisplayMode.Friends;

        }

        public void ShowFollowers()
        {
            CurrentDisplayMode = DisplayMode.Friends;
        }

        enum DisplayMode
        {
            None,
            Statuses,
            Friends,
        }
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
            source.GetFriendship(source_id: source.Id, target_id: target.Id)
                  .Subscribe(
                      r =>
                      {
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
