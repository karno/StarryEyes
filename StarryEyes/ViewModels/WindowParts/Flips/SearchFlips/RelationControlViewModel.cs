using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Requests;
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
        private bool _isNoRetweets;

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

        public bool IsNoRetweets
        {
            get { return this._isNoRetweets; }
            set
            {
                this._isNoRetweets = value;
                RaisePropertyChanged();
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
            this.IsNoRetweets = rds.IsNoRetweets(target.Id);
            Task.Run(() => this.GetFriendship(rds));
        }

        private async void GetFriendship(AccountRelationData rds)
        {
            try
            {
                // ReSharper disable InvertIf
                var fs = await _source.ShowFriendshipAsync(_source.Id, _target.Id);
                if (this.IsFollowing != fs.IsSourceFollowingTarget)
                {
                    this.IsFollowing = fs.IsSourceFollowingTarget;
                    await rds.SetFollowingAsync(this._target.Id, fs.IsSourceFollowingTarget);
                }
                if (this.IsFollowedBack != fs.IsTargetFollowingSource)
                {
                    this.IsFollowedBack = fs.IsTargetFollowingSource;
                    await rds.SetFollowerAsync(_target.Id, fs.IsTargetFollowingSource);
                }
                if (this.IsBlocking != fs.IsBlocking)
                {
                    this.IsBlocking = fs.IsBlocking;
                    await rds.SetBlockingAsync(_target.Id, fs.IsBlocking);
                }
                var nort = !fs.IsWantRetweets.GetValueOrDefault(true);
                if (this.IsNoRetweets != nort)
                {
                    this.IsNoRetweets = nort;
                    await rds.SetNoRetweetsAsync(_target.Id, nort);
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
                () =>
                {
                    this.IsFollowing = true;
                    Task.Run(() => _source.RelationData.SetFollowingAsync(_target.Id, true));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "フォロー エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "フォローできませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                })));
        }

        public void Remove()
        {
            this.DispatchAction(
                RelationKind.Unfollow,
                () =>
                {
                    this.IsFollowing = false;
                    Task.Run(() => _source.RelationData.SetFollowingAsync(_target.Id, false));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "アンフォロー エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "フォロー解除できませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
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
                    Task.Run(() => _source.RelationData.SetFollowingAsync(_target.Id, false));
                    Task.Run(() => _source.RelationData.SetBlockingAsync(_target.Id, true));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "ブロック エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "ブロックできませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                })));
        }

        public void Unblock()
        {
            this.DispatchAction(
                RelationKind.Unblock,
                () =>
                {
                    this.IsBlocking = false;
                    Task.Run(() => _source.RelationData.SetBlockingAsync(_target.Id, false));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "アンブロック エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "ブロックを解除できませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                })));
        }

        public void SuppressRetweets()
        {
            this.DispatchRetweetSuppression(
                true,
                () =>
                {
                    this.IsNoRetweets = true;
                    Task.Run(() => _source.RelationData.SetNoRetweetsAsync(_target.Id, true));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "リツイート非表示 エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "このユーザのリツイートを非表示にできませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                })));
        }

        public void UnsuppressRetweets()
        {
            this.DispatchRetweetSuppression(
                false,
                () =>
                {
                    this.IsNoRetweets = false;
                    Task.Run(() => _source.RelationData.SetNoRetweetsAsync(_target.Id, false));
                },
                ex => this._parent.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "リツイート非表示解除 エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "このユーザのリツイート非表示を解除できませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
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
                    Title = "スパム報告 エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "スパム報告できませんでした。",
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                })));
        }

        private string GetExceptionDescription(Exception ex)
        {
            var tex = ex as TwitterApiException;
            if (tex != null)
            {
                return "Twitter API エラー: " + tex.Message + Environment.NewLine +
                       "ステータスコード: " + tex.StatusCode + Environment.NewLine +
                       "Twitter エラーコード: " + (tex.TwitterErrorCode.HasValue
                           ? tex.TwitterErrorCode.Value.ToString()
                           : "なし");
            }
            var wex = ex as WebException;
            if (wex != null)
            {
                return "Web エラー: " + wex.Message + Environment.NewLine +
                       "ステータスコード: " + wex.Status;
            }
            return ex.Message;
        }

        private void DispatchRetweetSuppression(bool suppress, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            RequestQueue.Enqueue(_source, new UpdateFriendshipsRequest(_target, suppress))
                        .Finally(() => this.IsCommunicating = false)
                        .Subscribe(_ => { },
                                   failed,
                                   succeeded);
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