using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Requests;
using StarryEyes.Models.Stores;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class RelationControlViewModel : ViewModel
    {
        private readonly UserInfoViewModel _parent;
        private readonly TwitterAccount _source;
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
            get { return this._source.UnreliableProfileImage; }
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

        public RelationControlViewModel(UserInfoViewModel parent, TwitterAccount source, TwitterUser target)
        {
            this._parent = parent;
            this._source = source;
            this._target = target;
            var rds = source.RelationData;
            this.IsFollowing = rds.IsFollowing(target.Id);
            this.IsFollowedBack = rds.IsFollowedBy(target.Id);
            this.IsBlocking = rds.IsBlocking(target.Id);
            Task.Run(() => this.GetFriendship(rds));
        }

        private async void GetFriendship(AccountRelationData rds)
        {
            try
            {
                // ReSharper disable InvertIf
                var fs = await _source.ShowFriendship(_source.Id, _target.Id);
                if (this.IsFollowing != fs.IsSourceFollowingTarget)
                {
                    this.IsFollowing = fs.IsSourceFollowingTarget;
                    if (fs.IsSourceFollowingTarget)
                    {
                        rds.AddFollowing(_target.Id);
                    }
                    else
                    {
                        rds.RemoveFollowing(_target.Id);
                    }

                }

                if (this.IsFollowedBack != fs.IsTargetFollowingSource)
                {
                    this.IsFollowedBack = fs.IsTargetFollowingSource;
                    if (fs.IsTargetFollowingSource)
                    {
                        rds.AddFollower(_target.Id);
                    }
                    else
                    {
                        rds.RemoveFollower(_target.Id);
                    }
                }
                if (this.IsBlocking != fs.IsBlocking)
                {
                    this.IsBlocking = fs.IsBlocking;
                    if (fs.IsBlocking)
                    {
                        rds.AddBlocking(_target.Id);
                    }
                    else
                    {
                        rds.RemoveBlocking(_target.Id);
                    }
                }
                // ReSharper restore InvertIf
            }
            catch (Exception)
            {
                this.Enabled = false;
            }
        }

        public void Follow()
        {
            this.DispatchAction(
                RelationKind.Follow,
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
                RelationKind.Unfollow,
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
                RelationKind.Block,
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
                RelationKind.Unblock,
                () => this.IsBlocking = false,
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
                RelationKind.ReportAsSpam,
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

        private void DispatchAction(RelationKind work, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            RequestQueue.Enqueue(_source, new UpdateRelationRequest(_target, work))
                        .Finally(() => this.IsCommunicating = false)
                        .Subscribe(_ => { },
                                   failed,
                                   succeeded);
        }
    }
}