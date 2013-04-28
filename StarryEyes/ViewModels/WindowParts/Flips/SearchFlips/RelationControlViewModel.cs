using System;
using System.Reactive;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class RelationControlViewModel : ViewModel
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
            get { return this._source.UnreliableScreenName; }
        }

        public Uri SourceUserProfileImage
        {
            get { return this._source.UnreliableProfileImageUri; }
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

        public RelationControlViewModel(UserInfoViewModel parent, AuthenticateInfo source, TwitterUser target)
        {
            this._parent = parent;
            this._source = source;
            this._target = target;
            var rds = source.GetRelationData();
            this.IsFollowing = rds.IsFollowing(target.Id);
            this.IsFollowedBack = rds.IsFollowedBy(target.Id);
            this.IsBlocking = rds.IsBlocking(target.Id);
            source.GetFriendship(source.Id, target_id: target.Id)
                  .Subscribe(
                      r =>
                      {
                          // ReSharper disable InvertIf
                          if (this.IsFollowing != r.relationship.source.following)
                          {
                              this.IsFollowing = r.relationship.source.following;
                              if (r.relationship.source.following)
                              {
                                  rds.AddFollowing(target.Id);
                              }
                              else
                              {
                                  rds.RemoveFollowing(target.Id);
                              }
                          }
                          if (this.IsFollowedBack != r.relationship.source.followed_by)
                          {
                              this.IsFollowedBack = r.relationship.source.followed_by;
                              if (r.relationship.source.followed_by)
                              {
                                  rds.AddFollower(target.Id);
                              }
                              else
                              {
                                  rds.RemoveFollower(target.Id);
                              }
                          }
                          if (this.IsBlocking != r.relationship.source.blocking)
                          {
                              this.IsBlocking = r.relationship.source.blocking;
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
                          this.Enabled = false;
                      });
        }

        public void Follow()
        {
            this.DispatchAction(
                () =>
                this._source.CreateFriendship(this._target.Id)
                    .Select(_ => new Unit()),
                () => this.IsFollowing = true,
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
                this._source.DestroyFriendship(this._target.Id)
                    .Select(_ => new Unit()),
                () => this.IsFollowing = false,
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
                this._source.CreateBlock(this._target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    this.IsFollowing = false;
                    this.IsBlocking = true;
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
                this._source.DestroyBlock(this._target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    this.IsBlocking = false;
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
                this._source.ReportSpam(this._target.Id)
                    .Select(_ => new Unit()),
                () =>
                {
                    this.IsFollowing = false;
                    this.IsBlocking = true;
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
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
                  .Finally(() => this.IsCommunicating = false)
                  .Subscribe(_ => { },
                             failed,
                             succeeded);
        }
    }
}