using System;
using System.Globalization;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Globalization.WindowParts;
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
        private bool _isMutes;

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

        public bool IsMutes
        {
            get { return this._isMutes; }
            set
            {
                this._isMutes = value;
                RaisePropertyChanged();
            }
        }

        public RelationControlViewModel(UserInfoViewModel parent, TwitterAccount source, TwitterUser target)
        {
            this._parent = parent;
            this._source = source;
            this._target = target;
            var rds = source.RelationData;
            this.IsFollowing = rds.Followings.Contains(target.Id);
            this.IsFollowedBack = rds.Followers.Contains(target.Id);
            this.IsBlocking = rds.Blockings.Contains(target.Id);
            this.IsNoRetweets = rds.NoRetweets.Contains(target.Id);
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
                    await rds.Followings.SetAsync(this._target.Id, fs.IsSourceFollowingTarget);
                }
                if (this.IsFollowedBack != fs.IsTargetFollowingSource)
                {
                    this.IsFollowedBack = fs.IsTargetFollowingSource;
                    await rds.Followers.SetAsync(_target.Id, fs.IsTargetFollowingSource);
                }
                if (this.IsBlocking != fs.IsBlocking)
                {
                    this.IsBlocking = fs.IsBlocking;
                    await rds.Blockings.SetAsync(_target.Id, fs.IsBlocking);
                }
                var nort = !fs.IsWantRetweets.GetValueOrDefault(true);
                if (this.IsNoRetweets != nort)
                {
                    this.IsNoRetweets = nort;
                    await rds.NoRetweets.SetAsync(_target.Id, nort);
                }

                var mute = fs.IsMuting.GetValueOrDefault(false);
                if (this.IsMutes != mute)
                {
                    this.IsMutes = mute;
                    await rds.Mutes.SetAsync(_target.Id, mute);
                }
                // ReSharper restore InvertIf
            }
            catch (Exception)
            {
                this.Enabled = false;
            }
        }

        [UsedImplicitly]
        public void Follow()
        {
            this.DispatchAction(
                RelationKind.Follow,
                () =>
                {
                    this.IsFollowing = true;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorFollowTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorFollowInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void Remove()
        {
            this.DispatchAction(
                RelationKind.Unfollow,
                () =>
                {
                    this.IsFollowing = false;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnfollowTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnfollowInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void Block()
        {
            this.DispatchAction(
                RelationKind.Block,
                () =>
                {
                    this.IsFollowing = false;
                    this.IsBlocking = true;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, false));
                    Task.Run(() => _source.RelationData.Blockings.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorBlockTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorBlockInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void Unblock()
        {
            this.DispatchAction(
                RelationKind.Unblock,
                () =>
                {
                    this.IsBlocking = false;
                    Task.Run(() => _source.RelationData.Blockings.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnblockTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnblockInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void SuppressRetweets()
        {
            this.DispatchRetweetSuppression(
                true,
                () =>
                {
                    this.IsNoRetweets = true;
                    Task.Run(() => _source.RelationData.NoRetweets.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorSuppressRetweetsTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorSuppressRetweetsInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void UnsuppressRetweets()
        {
            this.DispatchRetweetSuppression(
                false,
                () =>
                {
                    this.IsNoRetweets = false;
                    Task.Run(() => _source.RelationData.NoRetweets.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnsuppressRetweetsTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnsuppressRetweetsInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void Mute()
        {
            this.DispatchMute(
                true,
                () =>
                {
                    this.IsMutes = true;
                    Task.Run(() => _source.RelationData.Mutes.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorMuteTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorMuteInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void Unmute()
        {
            this.DispatchMute(
                false,
                () =>
                {
                    this.IsMutes = false;
                    Task.Run(() => _source.RelationData.Mutes.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnmuteTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnmuteInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void ReportForSpam()
        {
            this.DispatchAction(
                RelationKind.ReportAsSpam,
                () =>
                {
                    this.IsFollowing = false;
                    this.IsBlocking = true;
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorReportSpamTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorReportSpamInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        private string GetExceptionDescription(Exception ex)
        {
            var tex = ex as TwitterApiException;
            if (tex != null)
            {
                return "Twitter API Error: " + tex.Message + Environment.NewLine +
                       "HTTP Status Code: " + tex.StatusCode + Environment.NewLine +
                       "Twitter Error Code: " + (tex.TwitterErrorCode.HasValue
                           ? tex.TwitterErrorCode.Value.ToString(CultureInfo.InvariantCulture)
                           : "None");
            }
            var wex = ex as WebException;
            if (wex != null)
            {
                return "Web Error: " + wex.Message + Environment.NewLine +
                       "HTTP Status Code: " + wex.Status;
            }
            return ex.Message;
        }

        private void DispatchRetweetSuppression(bool suppress, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            RequestQueue.EnqueueObservable(_source, new UpdateFriendshipsRequest(_target, null, suppress))
                        .Finally(() => this.IsCommunicating = false)
                        .Subscribe(_ => { }, failed, succeeded);
        }

        private void DispatchMute(bool mute, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            RequestQueue.EnqueueObservable(_source, new UpdateMuteRequest(_target, mute))
                        .Finally(() => this.IsCommunicating = false)
                        .Subscribe(_ => { }, failed, succeeded);
        }

        private void DispatchAction(RelationKind work, Action succeeded, Action<Exception> failed)
        {
            this.IsCommunicating = true;
            RequestQueue.EnqueueObservable(_source, new UpdateRelationRequest(_target, work))
                        .Finally(() => this.IsCommunicating = false)
                        .Subscribe(_ => { }, failed, succeeded);
        }

        private void ShowTaskDialogMessage(TaskDialogOptions options)
        {
            this._parent.Parent.Messenger.RaiseSafe(() => new TaskDialogMessage(options));
        }
    }
}