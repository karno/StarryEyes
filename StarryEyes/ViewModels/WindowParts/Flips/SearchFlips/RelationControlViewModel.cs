using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cadena.Api;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using Cadena.Engine;
using Cadena.Engine.Requests;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Subsystems;
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

        public string SourceUserScreenName => _source.UnreliableScreenName;

        public Uri SourceUserProfileImage => _source.UnreliableProfileImage;

        public bool IsCommunicating
        {
            get => _isCommunicating;
            set
            {
                _isCommunicating = value;
                RaisePropertyChanged();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                RaisePropertyChanged();
            }
        }

        public bool IsFollowing
        {
            get => _isFollowing;
            set
            {
                _isFollowing = value;
                RaisePropertyChanged();
            }
        }

        public bool IsFollowedBack
        {
            get => _isFollowedBack;
            set
            {
                _isFollowedBack = value;
                RaisePropertyChanged();
            }
        }

        public bool IsBlocking
        {
            get => _isBlocking;
            set
            {
                _isBlocking = value;
                RaisePropertyChanged();
            }
        }

        public bool IsNoRetweets
        {
            get => _isNoRetweets;
            set
            {
                _isNoRetweets = value;
                RaisePropertyChanged();
            }
        }

        public bool IsMutes
        {
            get => _isMutes;
            set
            {
                _isMutes = value;
                RaisePropertyChanged();
            }
        }

        public RelationControlViewModel(UserInfoViewModel parent, TwitterAccount source, TwitterUser target)
        {
            _parent = parent;
            _source = source;
            _target = target;
            var rds = source.RelationData;
            IsFollowing = rds.Followings.Contains(target.Id);
            IsFollowedBack = rds.Followers.Contains(target.Id);
            IsBlocking = rds.Blockings.Contains(target.Id);
            IsNoRetweets = rds.NoRetweets.Contains(target.Id);
            Task.Run(() => GetFriendship(rds));
        }

        private async void GetFriendship(AccountRelationData rds)
        {
            try
            {
                // ReSharper disable InvertIf
                var fs = (await _source.CreateAccessor().ShowFriendshipAsync(new UserParameter(_source.Id),
                    new UserParameter(_target.Id), CancellationToken.None)).Result;
                if (IsFollowing != fs.IsSourceFollowingTarget)
                {
                    IsFollowing = fs.IsSourceFollowingTarget;
                    await rds.Followings.SetAsync(_target.Id, fs.IsSourceFollowingTarget);
                }
                if (IsFollowedBack != fs.IsTargetFollowingSource)
                {
                    IsFollowedBack = fs.IsTargetFollowingSource;
                    await rds.Followers.SetAsync(_target.Id, fs.IsTargetFollowingSource);
                }
                if (IsBlocking != fs.IsBlocking)
                {
                    IsBlocking = fs.IsBlocking;
                    await rds.Blockings.SetAsync(_target.Id, fs.IsBlocking);
                }
                var nort = !fs.IsWantRetweets.GetValueOrDefault(true);
                if (IsNoRetweets != nort)
                {
                    IsNoRetweets = nort;
                    await rds.NoRetweets.SetAsync(_target.Id, nort);
                }

                var mute = fs.IsMuting.GetValueOrDefault(false);
                if (IsMutes != mute)
                {
                    IsMutes = mute;
                    await rds.Mutes.SetAsync(_target.Id, mute);
                }
                // ReSharper restore InvertIf
            }
            catch (Exception)
            {
                Enabled = false;
            }
        }

        [UsedImplicitly]
        public void Follow()
        {
            DispatchAction(
                Relationships.Follow,
                () =>
                {
                    IsFollowing = true;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorFollowTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorFollowInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void Remove()
        {
            DispatchAction(
                Relationships.Unfollow,
                () =>
                {
                    IsFollowing = false;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnfollowTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnfollowInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void Block()
        {
            DispatchAction(
                Relationships.Block,
                () =>
                {
                    IsFollowing = false;
                    IsBlocking = true;
                    Task.Run(() => _source.RelationData.Followings.SetAsync(_target.Id, false));
                    Task.Run(() => _source.RelationData.Blockings.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorBlockTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorBlockInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void Unblock()
        {
            DispatchAction(
                Relationships.Unblock,
                () =>
                {
                    IsBlocking = false;
                    Task.Run(() => _source.RelationData.Blockings.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnblockTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnblockInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void SuppressRetweets()
        {
            DispatchRetweetSuppression(
                true,
                () =>
                {
                    IsNoRetweets = true;
                    Task.Run(() => _source.RelationData.NoRetweets.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorSuppressRetweetsTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorSuppressRetweetsInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void UnsuppressRetweets()
        {
            DispatchRetweetSuppression(
                false,
                () =>
                {
                    IsNoRetweets = false;
                    Task.Run(() => _source.RelationData.NoRetweets.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnsuppressRetweetsTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnsuppressRetweetsInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void Mute()
        {
            DispatchMute(
                true,
                () =>
                {
                    IsMutes = true;
                    Task.Run(() => _source.RelationData.Mutes.SetAsync(_target.Id, true));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorMuteTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorMuteInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void Unmute()
        {
            DispatchMute(
                false,
                () =>
                {
                    IsMutes = false;
                    Task.Run(() => _source.RelationData.Mutes.SetAsync(_target.Id, false));
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorUnmuteTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorUnmuteInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        [UsedImplicitly]
        public void ReportForSpam()
        {
            DispatchAction(
                Relationships.ReportAsSpam,
                () =>
                {
                    IsFollowing = false;
                    IsBlocking = true;
                },
                ex => ShowTaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgErrorReportSpamTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = SearchFlipResources.MsgErrorReportSpamInst,
                    Content = GetExceptionDescription(ex),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
        }

        private string GetExceptionDescription(Exception ex)
        {
            var tex = ex as TwitterApiException;
            if (tex != null)
            {
                return "Twitter API Error: " + tex.Message + Environment.NewLine +
                       "HTTP Status Code: " + tex.StatusCode + Environment.NewLine +
                       "Twitter Error Code: " + (tex.TwitterErrorCode?.ToString() ?? "None");
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
            DispatchRequest(new UpdateFriendshipRequest(_source.CreateAccessor(),
                new UserParameter(_target.Id), null, suppress), succeeded, failed);
        }

        private void DispatchMute(bool mute, Action succeeded, Action<Exception> failed)
        {
            DispatchRequest(new UpdateMuteRequest(_source.CreateAccessor(),
                new UserParameter(_target.Id), mute), succeeded, failed);
        }

        private void DispatchAction(Relationships relation, Action succeeded, Action<Exception> failed)
        {
            DispatchRequest(new UpdateRelationRequest(_source.CreateAccessor(),
                new UserParameter(_target.Id), relation), succeeded, failed);
        }

        private async void DispatchRequest<T>(IRequest<T> request, Action succeeded,
            Action<Exception> failed)
        {
            IsCommunicating = true;
            try
            {
                await RequestManager.Enqueue(request).ConfigureAwait(false);
                succeeded();
            }
            catch (Exception ex)
            {
                failed(ex);
            }
            finally
            {
                IsCommunicating = false;
            }
        }

        private void ShowTaskDialogMessage(TaskDialogOptions options)
        {
            _parent.Parent.Messenger.RaiseSafe(() => new TaskDialogMessage(options));
        }
    }
}