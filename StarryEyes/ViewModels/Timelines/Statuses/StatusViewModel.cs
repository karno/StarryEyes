using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Filters.Expressions.Values.Immediates;
using StarryEyes.Filters.Expressions.Values.Statuses;
using StarryEyes.Filters.Expressions.Values.Users;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Inputting;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Requests;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Properties;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;
using StarryEyes.Views.Messaging;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.Timelines.Statuses
{
    public class StatusViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<UserViewModel> _favoritedUsers;
        private readonly TimelineViewModelBase _parent;
        private readonly ReadOnlyDispatcherCollectionRx<UserViewModel> _retweetedUsers;
        private readonly ReadOnlyDispatcherCollectionRx<ThumbnailImageViewModel> _images;
        private readonly bool _isInReplyToExists;
        private long[] _bindingAccounts;
        private TwitterStatus _inReplyTo;
        private bool _isSelected;
        private UserViewModel _recipient;
        private UserViewModel _retweeter;
        private UserViewModel _user;
        private bool _isInReplyToLoading;
        private bool _isInReplyToLoaded;

        public StatusViewModel(TimelineViewModelBase parent, StatusModel status,
            IEnumerable<long> initialBoundAccounts)
        {
            _parent = parent;
            // get status model
            Model = status;
            RetweetedOriginalModel = status.RetweetedOriginal;

            // bind accounts 
            _bindingAccounts = initialBoundAccounts.Guard().ToArray();

            // initialize users information
            CompositeDisposable.Add(
                _favoritedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    Model.FavoritedUsers, user => new UserViewModel(user),
                    DispatcherHelper.UIDispatcher, DispatcherPriority.Background));
            CompositeDisposable.Add(
                _favoritedUsers.ListenCollectionChanged(_ =>
                {
                    RaisePropertyChanged(() => IsFavorited);
                    RaisePropertyChanged(() => IsFavoritedUserExists);
                    RaisePropertyChanged(() => FavoriteCount);
                }));
            CompositeDisposable.Add(
                _retweetedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    Model.RetweetedUsers, user => new UserViewModel(user),
                    DispatcherHelper.UIDispatcher, DispatcherPriority.Background));
            CompositeDisposable.Add(
                _retweetedUsers.ListenCollectionChanged(_ =>
                    {
                        RaisePropertyChanged(() => IsRetweeted);
                        RaisePropertyChanged(() => IsRetweetedUserExists);
                        RaisePropertyChanged(() => RetweetCount);
                    }));

            if (RetweetedOriginalModel != null)
            {
                CompositeDisposable.Add(
                    RetweetedOriginalModel.FavoritedUsers.ListenCollectionChanged(
                        _ => RaisePropertyChanged(() => IsFavorited)));
                CompositeDisposable.Add(
                    RetweetedOriginalModel.RetweetedUsers.ListenCollectionChanged(
                        _ => RaisePropertyChanged(() => IsRetweeted)));
            }

            // listen settings
            CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => Setting.AllowFavoriteMyself.ValueChanged += h,
                    h => Setting.AllowFavoriteMyself.ValueChanged -= h,
                    _ => RaisePropertyChanged(() => CanFavorite)));
            CompositeDisposable.Add(
                new EventListener<Action<ThumbnailMode>>(
                    h => Setting.ThumbnailMode.ValueChanged += h,
                    h => Setting.ThumbnailMode.ValueChanged -= h,
                    _ =>
                    {
                        RaisePropertyChanged(() => IsThumbnailAvailable);
                        RaisePropertyChanged(() => IsThumbnailsAvailable);
                    }));
            CompositeDisposable.Add(
                new EventListener<Action<TweetDisplayMode>>(
                    h => Setting.TweetDisplayMode.ValueChanged += h,
                    h => Setting.TweetDisplayMode.ValueChanged -= h,
                    _ =>
                    {
                        RaisePropertyChanged(() => IsExpanded);
                        RaisePropertyChanged(() => IsSingleLine);
                    }));
            // when account is added/removed, all timelines are regenerated.
            // so, we don't have to listen any events which notify accounts addition/deletion.

            CompositeDisposable.Add(_images = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                Model.Images, m => new ThumbnailImageViewModel(m), DispatcherHelper.UIDispatcher));
            // resolve images
            CompositeDisposable.Add(_images.ListenCollectionChanged(_ =>
            {
                RaisePropertyChanged(() => ThumbnailImage);
                RaisePropertyChanged(() => IsImageAvailable);
                RaisePropertyChanged(() => IsThumbnailAvailable);
                RaisePropertyChanged(() => IsThumbnailsAvailable);
            }));

            // look-up in-reply-to
            _isInReplyToExists = Status.InReplyToStatusId.HasValue && Status.InReplyToStatusId != 0;
        }

        public TimelineViewModelBase Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///     Represents status model.
        /// </summary>
        public StatusModel Model { get; private set; }

        public StatusModel RetweetedOriginalModel { get; private set; }

        /// <summary>
        ///     Represents ORIGINAL status. 
        ///     (if this status is retweet, this property represents a status which contains retweeted_original.)
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
            get { return Model.Status.RetweetedOriginal ?? Model.Status; }
        }

        public IEnumerable<long> BindingAccounts
        {
            get { return _bindingAccounts; }
            set
            {
                _bindingAccounts = (value as long[]) ?? value.ToArray();
                // raise property changed
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsFavorited);
                RaisePropertyChanged(() => IsRetweeted);
                RaisePropertyChanged(() => IsMyselfStrict);
            }
        }

        public UserViewModel User
        {
            get
            {
                return _user ??
                       (_user = CreateUserViewModel((Status.RetweetedOriginal ?? Status).User));
            }
        }

        public UserViewModel Retweeter
        {
            get
            {
                if (!IsRetweet)
                {
                    return null;
                }
                return _retweeter ?? (_retweeter = CreateUserViewModel(OriginalStatus.User));

            }
        }

        public UserViewModel Recipient
        {
            get
            {
                if (!IsDirectMessage)
                {
                    return null;
                }
                return _recipient ?? (_recipient = CreateUserViewModel(Status.Recipient));
            }
        }

        private UserViewModel CreateUserViewModel(TwitterUser user)
        {
            var uvm = new UserViewModel(user);
            try
            {
                CompositeDisposable.Add(uvm);
                return uvm;
            }
            catch (ObjectDisposedException)
            {
                // release all subscriptions
                uvm.Dispose();
                return uvm;
            }
        }

        public string MultiLineText
        {
            get { return Status.GetEntityAidedText(); }
        }

        public string SingleLineText
        {
            get { return Status.GetEntityAidedText().Replace('\n', ' ').Replace("\r", ""); }
        }

        public bool IsRetweetedUserExists
        {
            get { return _retweetedUsers.Count > 0; }
        }

        public int RetweetCount
        {
            get { return RetweetedUsers.Count; }
        }

        public ReadOnlyDispatcherCollectionRx<UserViewModel> RetweetedUsers
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

        public ReadOnlyDispatcherCollectionRx<UserViewModel> FavoritedUsers
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
            get
            {
                return RetweetedOriginalModel != null
                    ? RetweetedOriginalModel.IsFavorited(_bindingAccounts)
                    : Model.IsFavorited(_bindingAccounts);
            }
        }

        public bool IsRetweeted
        {
            get
            {
                return RetweetedOriginalModel != null
                    ? RetweetedOriginalModel.IsRetweeted(_bindingAccounts)
                    : Model.IsRetweeted(_bindingAccounts);
            }
        }

        public bool CanFavoriteAndRetweet
        {
            get { return CanFavoriteImmediate && CanRetweetImmediate; }
        }

        public bool CanFavorite
        {
            get { return !IsDirectMessage && (Setting.AllowFavoriteMyself.Value || !IsMyself); }
        }

        public bool CanFavoriteImmediate
        {
            get { return CanFavorite; }
        }

        public bool CanRetweet
        {
            get { return !IsDirectMessage && !Status.User.IsProtected; }
        }

        public bool CanRetweetImmediate
        {
            get { return CanRetweet; }
        }

        public bool CanDelete
        {
            get { return IsDirectMessage || Setting.Accounts.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyself
        {
            get { return Setting.Accounts.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get { return CheckUserIsBind(Status.User.Id); }
        }

        private bool CheckUserIsBind(long id)
        {
            return _bindingAccounts.Length == 1 && _bindingAccounts[0] == id;
        }

        public bool IsInReplyToMe
        {
            get
            {
                return FilterSystemUtil.InReplyToUsers(Status)
                                       .Any(Setting.Accounts.Contains);
            }
        }

        public bool IsFocused
        {
            get { return _parent.FocusedStatus == this; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value || Parent == null) return;
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);
                Parent.OnSelectionUpdated();
            }
        }

        public bool IsExpanded
        {
            get
            {
                switch (Setting.TweetDisplayMode.Value)
                {
                    case TweetDisplayMode.SingleLine:
                    case TweetDisplayMode.MultiLine:
                        return false;
                    case TweetDisplayMode.MixedSingleLine:
                    case TweetDisplayMode.MixedMultiLine:
                        return IsFocused;
                    default:
                        return true;
                }
            }
        }

        public bool IsSingleLine
        {
            get
            {
                switch (Setting.TweetDisplayMode.Value)
                {
                    case TweetDisplayMode.SingleLine:
                    case TweetDisplayMode.MixedSingleLine:
                        return true;
                    case TweetDisplayMode.MultiLine:
                    case TweetDisplayMode.MixedMultiLine:
                        return false;
                    default:
                        return true;
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
                if (Status.Source == null)
                {
                    return String.Empty;
                }
                if (!IsSourceIsLink)
                {
                    return Status.Source;
                }
                var start = Status.Source.IndexOf(">", StringComparison.Ordinal);
                var end = Status.Source.IndexOf("<", start + 1, StringComparison.Ordinal);
                if (start >= 0 && end >= 0)
                {
                    return Status.Source.Substring(start + 1, end - start - 1);
                }
                return Status.Source;
            }
        }

        public DateTime CreatedAt
        {
            get { return Status.CreatedAt; }
        }

        public bool IsImageAvailable
        {
            get { return Model.Images != null && Model.Images.Any(); }
        }

        public ReadOnlyDispatcherCollectionRx<ThumbnailImageViewModel> Images
        {
            get { return _images; }
        }

        public bool IsThumbnailAvailable
        {
            get { return IsImageAvailable && Setting.ThumbnailMode.Value == ThumbnailMode.Single; }
        }

        public bool IsThumbnailsAvailable
        {
            get { return IsImageAvailable && Setting.ThumbnailMode.Value == ThumbnailMode.All; }
        }

        public ThumbnailImageViewModel ThumbnailImage
        {
            get { return Model.Images != null ? Images.FirstOrDefault() : null; }
        }

        /// <summary>
        ///     For animating helper
        /// </summary>
        internal bool IsLoaded { get; set; }

        public void RaiseFocusedChanged()
        {
            RaisePropertyChanged(() => IsFocused);
            RaisePropertyChanged(() => IsExpanded);
            if (IsFocused)
            {
                LoadInReplyTo();
            }
        }

        public void ShowUserProfile()
        {
            SearchFlipModel.RequestSearch(User.ScreenName, SearchMode.UserScreenName);
        }

        public void ShowRetweeterProfile()
        {
            SearchFlipModel.RequestSearch(Retweeter.ScreenName, SearchMode.UserScreenName);
        }

        public void ShowRecipientProfile()
        {
            SearchFlipModel.RequestSearch(Recipient.ScreenName, SearchMode.UserScreenName);
        }

        public void OpenWeb()
        {
            BrowserHelper.Open(Status.Permalink);
        }

        public void OpenFavstar()
        {
            BrowserHelper.Open(Status.FavstarPermalink);
        }

        public void OpenUserDetailOnTwitter()
        {
            User.OpenUserDetailOnTwitter();
        }

        public void OpenUserFavstar()
        {
            User.OpenUserFavstar();
        }

        public void OpenUserTwilog()
        {
            User.OpenUserTwilog();
        }

        public void OpenSourceLink()
        {
            if (Status.Source == null || !IsSourceIsLink) return;
            var start = Status.Source.IndexOf("\"", StringComparison.Ordinal);
            var end = Status.Source.IndexOf("\"", start + 1, StringComparison.Ordinal);
            if (start < 0 || end < 0) return;
            var url = Status.Source.Substring(start + 1, end - start - 1);
            BrowserHelper.Open(url);
        }

        #region Reply Control

        private void NotifyChangeReplyInfo()
        {
            RaisePropertyChanged(() => IsInReplyToExists);
            RaisePropertyChanged(() => IsInReplyToLoaded);
            RaisePropertyChanged(() => IsInReplyToLoading);
            RaisePropertyChanged(() => IsInReplyToAvailable);
            RaisePropertyChanged(() => InReplyToUserImage);
            RaisePropertyChanged(() => InReplyToUserName);
            RaisePropertyChanged(() => InReplyToUserScreenName);
            RaisePropertyChanged(() => InReplyToBody);
        }

        public bool IsInReplyToExists
        {
            get { return _isInReplyToExists; }
        }

        public bool IsInReplyToLoaded
        {
            get { return _isInReplyToLoaded; }
        }

        public bool IsInReplyToLoading
        {
            get { return _isInReplyToLoading; }
        }

        public bool IsInReplyToAvailable
        {
            get { return _inReplyTo != null; }
        }

        public Uri InReplyToUserImage
        {
            get
            {
                if (_inReplyTo == null) return null;
                return _inReplyTo.User.ProfileImageUri;
            }
        }

        public string InReplyToUserName
        {
            get
            {
                if (_inReplyTo == null) return null;
                return _inReplyTo.User.Name;
            }
        }

        public string InReplyToUserScreenName
        {
            get
            {
                if (_inReplyTo == null)
                    return Status.InReplyToScreenName;
                return _inReplyTo.User.ScreenName;
            }
        }

        public string InReplyToBody
        {
            get
            {
                if (_inReplyTo == null) return null;
                return _inReplyTo.Text;
            }
        }

        private async void LoadInReplyTo()
        {
            if (_isInReplyToLoading || _isInReplyToLoaded) return;
            var inReplyToStatusId = Status.InReplyToStatusId;
            if (inReplyToStatusId == null)
            {
                _isInReplyToLoaded = true;
                RaisePropertyChanged(() => IsInReplyToLoaded);
                return;
            }
            _isInReplyToLoading = true;
            RaisePropertyChanged(() => IsInReplyToLoading);
            try
            {
                _inReplyTo = await StoreHelper.GetTweetAsync(inReplyToStatusId.Value).ConfigureAwait(false);
                _isInReplyToLoaded = true;
                _isInReplyToLoading = false;
                NotifyChangeReplyInfo();
            }
            catch (Exception)
            {
                _isInReplyToLoading = false;
            }
        }

        #endregion

        #region Text selection control

        private string _selectedText;

        public string SelectedText
        {
            get { return _selectedText ?? String.Empty; }
            set
            {
                _selectedText = value;
                RaisePropertyChanged();
            }
        }

        public void CopyText()
        {
            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                Clipboard.SetText(SelectedText);
            }
            catch
            {
            }
            // ReSharper restore EmptyGeneralCatchClause
        }

        public void SetTextToInputBox()
        {
            InputModel.InputCore.SetText(InputSetting.Create(SelectedText));
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

        #region Execution commands

        public void CopyBody()
        {
            SetClipboard(Status.GetEntityAidedText(EntityDisplayMode.LinkUri));
        }

        public void CopyPermalink()
        {
            SetClipboard(Status.Permalink);
        }

        public void CopySTOT()
        {
            SetClipboard(Status.STOTString);
        }

        private void SetClipboard(string value)
        {
            try
            {
                Clipboard.SetText(value);
            }
            catch (Exception ex)
            {
                Parent.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = MainAreaTimelineResources.MsgClipboardErrorTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = MainAreaTimelineResources.MsgClipboardErrorInst,
                    Content = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
            }
        }

        public void Favorite(IEnumerable<TwitterAccount> infos, bool add)
        {
            if (IsDirectMessage)
            {
                // disable on direct messages
                return;
            }
            Task.Run(() =>
            {
                Action<TwitterAccount> expected;
                Action<TwitterAccount> onFail;
                if (add)
                {
                    expected = a => Model.AddFavoritedUser(a.GetPseudoUser());
                    onFail = a => Model.RemoveFavoritedUser(a.Id);
                }
                else
                {
                    expected = a => Model.RemoveFavoritedUser(a.Id);
                    onFail = a => Model.AddFavoritedUser(a.GetPseudoUser());
                }

                var request = new FavoriteRequest(Status, add);

                // define working task
                Func<TwitterAccount, Task> workTask = account => Task.Run(async () =>
                {
                    expected(account);
                    try
                    {
                        await RequestQueue.EnqueueAsync(account, request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        onFail(account);
                        var desc = add
                            ? MainAreaTimelineResources.MsgFavoriteFailed
                            : MainAreaTimelineResources.MsgUnfavoriteFailed;
                        BackstageModel.RegisterEvent(new OperationFailedEvent(
                            desc + "(" + account.UnreliableScreenName + " -> " +
                            Status.User.ScreenName + ")", ex));
                    }
                });

                // dispatch actions
                Task.WaitAll(infos.Select(workTask).ToArray());

                // notify changed
                RaisePropertyChanged(() => IsFavorited);
            });
        }

        public void Retweet(IEnumerable<TwitterAccount> infos, bool add)
        {
            if (IsDirectMessage)
            {
                // disable on direct messages
                return;
            }
            Task.Run(() =>
            {
                Action<TwitterAccount> expected;
                Action<TwitterAccount> onFail;
                if (add)
                {
                    expected = a => Model.AddRetweetedUser(a.GetPseudoUser());
                    onFail = a => Model.RemoveRetweetedUser(a.Id);
                }
                else
                {
                    expected = a => Model.RemoveRetweetedUser(a.Id);
                    onFail = a => Model.AddRetweetedUser(a.GetPseudoUser());
                }
                var request = new RetweetRequest(Status, add);

                // define working task
                Func<TwitterAccount, Task> workTask = account => Task.Run(async () =>
                {
                    expected(account);
                    try
                    {
                        await RequestQueue.EnqueueAsync(account, request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        onFail(account);
                        var desc = add
                            ? MainAreaTimelineResources.MsgRetweetFailed
                            : MainAreaTimelineResources.MsgUnretweetFailed;
                        BackstageModel.RegisterEvent(new OperationFailedEvent(
                            desc + "(" + account.UnreliableScreenName + " -> " +
                            Status.User.ScreenName + ")", ex));
                    }
                });

                // dispatch actions
                Task.WaitAll(infos.Select(workTask).ToArray());

                // notify changed
                RaisePropertyChanged(() => IsRetweeted);
            });
        }

        public void ToggleFavoriteImmediate()
        {
            if (!AssertQuickActionEnabled()) return;
            if (IsDirectMessage)
            {
                NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    MainAreaTimelineResources.MsgProhibitFavoriteDirectMessage);
                return;
            }
            if (!CanFavoriteImmediate && !IsFavorited)
            {
                NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    MainAreaTimelineResources.MsgProhibitFavoriteMyself);
                return;
            }
            Favorite(GetImmediateAccounts(), !IsFavorited);
        }

        public void ToggleRetweetImmediate()
        {
            if (!AssertQuickActionEnabled()) return;
            if (!CanRetweetImmediate)
            {
                if (IsMyselfStrict)
                {
                    NotifyQuickActionFailed(
                        MainAreaTimelineResources.MsgProhibitRetweet,
                        MainAreaTimelineResources.MsgProhibitRetweetMyself);
                }
                else
                {
                    NotifyQuickActionFailed(
                        MainAreaTimelineResources.MsgProhibitRetweet,
                        MainAreaTimelineResources.MsgProhibitRetweetDirectMessage);
                }
                return;
            }
            Retweet(GetImmediateAccounts(), !IsRetweeted);
        }

        private bool AssertQuickActionEnabled()
        {
            if (BindingAccounts.Any()) return true;
            NotifyQuickActionFailed(
                MainAreaTimelineResources.MsgQuickActionAccountIsNotSelected,
                MainAreaTimelineResources.MsgQuickActionAccountIsNotSelectedDetail);
            return false;
        }

        private void NotifyQuickActionFailed(string main, string body)
        {
            Parent.Messenger.RaiseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = MainAreaTimelineResources.MsgQuickActionFailedTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = main,
                    Content = body,
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
        }

        [UsedImplicitly]
        public void FavoriteAndRetweetImmediate()
        {
            if (IsDirectMessage)
            {
                // disable on direct messages
                return;
            }
            if (!AssertQuickActionEnabled()) return;
            var accounts = GetImmediateAccounts()
                               .ToObservable()
                               .Publish();
            if (!IsFavorited)
            {
                var freq = new FavoriteRequest(Status, true);
                accounts.Do(a => Task.Run(() => Model.AddFavoritedUser(a.GetPseudoUser())))
                        .Do(_ => RaisePropertyChanged(() => IsFavorited))
                        .SelectMany(a => RequestQueue.EnqueueObservable(a, freq)
                                                     .Catch((Exception ex) =>
                                                     {
                                                         Task.Run(() => Model.RemoveFavoritedUser(a.Id));
                                                         return Observable.Empty<TwitterStatus>();
                                                     }))
                        .Do(_ => RaisePropertyChanged(() => IsFavorited))
                        .Subscribe();
            }
            if (!IsRetweeted)
            {
                var rreq = new RetweetRequest(Status, true);
                accounts.Do(a => Task.Run(() => Model.AddRetweetedUser(a.GetPseudoUser())))
                        .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                        .SelectMany(a => RequestQueue.EnqueueObservable(a, rreq)
                                                     .Catch((Exception ex) =>
                                                     {
                                                         Task.Run(() => Model.RemoveRetweetedUser(a.Id));
                                                         return Observable.Empty<TwitterStatus>();
                                                     }))
                        .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                        .Subscribe();
            }
            accounts.Connect();
        }

        private IEnumerable<TwitterAccount> GetImmediateAccounts()
        {
            return Setting.Accounts.Collection.Where(a => _bindingAccounts.Contains(a.Id));
        }

        public void ToggleSelect()
        {
            IsSelected = !IsSelected;
        }

        public void ToggleFavorite()
        {
            if (!CanFavorite)
            {
                NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    IsDirectMessage
                        ? MainAreaTimelineResources.MsgProhibitFavoriteDirectMessage
                        : MainAreaTimelineResources.MsgProhibitFavoriteMyself);
                return;
            }
            var model = RetweetedOriginalModel ?? Model;
            var favoriteds =
                Setting.Accounts.Collection
                       .Where(a => model.IsFavorited(a.Id))
                       .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(
                AccountSelectionAction.Favorite,
                favoriteds,
                infos =>
                {
                    var accounts =
                        infos as TwitterAccount[] ?? infos.ToArray();
                    var adds = accounts.Except(favoriteds);
                    var rmvs = favoriteds.Except(accounts);
                    Favorite(adds, true);
                    Favorite(rmvs, false);
                });
        }

        public void ToggleRetweet()
        {
            if (!CanRetweet)
            {
                NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitRetweet,
                    IsDirectMessage
                        ? MainAreaTimelineResources.MsgProhibitRetweetDirectMessage
                        : MainAreaTimelineResources.MsgProhibitRetweetMyself);
                return;
            }
            var model = RetweetedOriginalModel ?? Model;
            var retweeteds = Setting.Accounts.Collection
                                    .Where(a => model.IsRetweeted(a.Id))
                                    .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(
                AccountSelectionAction.Retweet,
                retweeteds,
                infos =>
                {
                    var authenticateInfos =
                        infos as TwitterAccount[] ?? infos.ToArray();
                    var adds =
                        authenticateInfos.Except(retweeteds);
                    var rmvs =
                        retweeteds.Except(authenticateInfos);
                    Retweet(adds, true);
                    Retweet(rmvs, false);
                });
        }

        [UsedImplicitly]
        public void SendReplyOrDirectMessage()
        {
            if (IsDirectMessage)
            {
                DirectMessage();
            }
            else
            {
                Reply();
            }
        }

        public void SendReplyOrDirectMessage(string body)
        {
            if (IsDirectMessage)
            {
                DirectMessage(body);
            }
            else
            {
                Reply(body);
            }
        }

        private void Reply()
        {
            if (IsSelected)
            {
                Parent.ReplySelecteds();
                return;
            }
            InputModel.InputCore.SetText(InputSetting.CreateReply(Status));
        }

        private void Reply(string body)
        {
            // from key assign
            if (String.IsNullOrEmpty(body))
            {
                Reply();
                return;
            }
            try
            {
                var formatted = String.Format(body, User.ScreenName, User.Name);
                InputModel.InputCore.SetText(InputSetting.CreateReply(Status, formatted, false));
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("Reply format error: " + body, ex));
            }
        }

        public void Quote()
        {
            if (IsDirectMessage)
            {
                // disable on direct messages
                return;
            }
            var setting = InputSetting.CreateReply(Status,
                " RT @" + User.ScreenName + ": " + Status.GetEntityAidedText(EntityDisplayMode.LinkUri),
                false);
            setting.CursorPosition = CursorPosition.Begin;
            InputModel.InputCore.SetText(setting);
        }

        public void QuotePermalink()
        {
            if (IsDirectMessage)
            {
                // disable on direct messages
                return;
            }
            var setting = InputSetting.Create(Model.GetSuitableReplyAccount(),
                " " + Status.Permalink);
            setting.CursorPosition = CursorPosition.Begin;
            InputModel.InputCore.SetText(setting);
        }

        public void DirectMessage()
        {
            InputModel.InputCore.SetText(
                InputSetting.CreateDirectMessage(Model.GetSuitableReplyAccount(),
                    Status.User));
        }

        public void DirectMessage(string body)
        {
            try
            {
                var formatted = String.Format(body, User.ScreenName, User.Name);
                InputModel.InputCore.SetText(InputSetting.CreateDirectMessage(
                    Model.GetSuitableReplyAccount(), Status.User, formatted));
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("Direct Message format error: " + body, ex));
            }
        }

        public void ConfirmDelete()
        {
            var footer = MainAreaTimelineResources.MsgDeleteFooter;
            var amendkey = KeyAssignManager.CurrentProfile
                                           .FindAssignFromActionName("Amend", KeyAssignGroup.Input)
                                           .FirstOrDefault();
            if (amendkey != null)
            {
                footer = MainAreaTimelineResources.MsgDeleteFooterWithKeyFormat
                                                  .SafeFormat(amendkey.GetKeyDescribeString());
            }
            var response = Parent.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = MainAreaTimelineResources.MsgDeleteTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = MainAreaTimelineResources.MsgDeleteInst,
                    Content = MainAreaTimelineResources.MsgDeleteContent,
                    CustomButtons = new[] { MainAreaTimelineResources.MsgDeleteCmdDelete, Resources.MsgButtonCancel },
                    AllowDialogCancellation = true,
                    DefaultButtonIndex = 0,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = footer
                }));
            if (response.Response.CustomButtonResult == 0)
            {
                Delete();
            }
        }

        public void Delete()
        {
            TwitterAccount info;
            if (IsDirectMessage)
            {
                var ids = Status.Recipient == null
                    ? new[] { Status.User.Id }
                    : new[] { Status.User.Id, Status.Recipient.Id };
                info = ids
                    .Select(Setting.Accounts.Get).FirstOrDefault(_ => _ != null);
            }
            else
            {
                info = Setting.Accounts.Get(OriginalStatus.User.Id);
            }
            if (info == null) return;
            Task.Run(async () =>
            {
                var dreq = new DeletionRequest(OriginalStatus);
                try
                {
                    var result = await RequestQueue.EnqueueAsync(info, dreq).ConfigureAwait(false);
                    StatusInbox.EnqueueRemoval(result.Id);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent(
                        MainAreaTimelineResources.MsgTweetDeleteFailed, ex));
                }
            });
        }

        private bool _lastSelectState;

        public void ToggleFocus()
        {
            var psel = _lastSelectState;
            _lastSelectState = IsSelected;
            if (psel != IsSelected) return;
            // toggle focus
            Parent.FocusedStatus =
                Parent.FocusedStatus == this ? null : this;
            if (Parent.FocusedStatus == this)
            {
                LoadInReplyTo();
            }
            Parent.Focus();
        }

        [UsedImplicitly]
        public void Focus()
        {
            Parent.FocusedStatus = this;
            LoadInReplyTo();
            Parent.Focus();
        }

        public void ShowConversation()
        {
            SearchFlipModel.RequestSearch("?from conv:\"" + Status.Id + "\"", SearchMode.Local);
            Parent.FocusedStatus = null;
        }

        public void ReportAsSpam()
        {
            var response = Parent.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = MainAreaTimelineResources.MsgReportAsSpamTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = MainAreaTimelineResources.MsgReportAsSpamInstFormat
                                                               .SafeFormat("@" + Status.User.ScreenName),
                    Content = MainAreaTimelineResources.MsgReportAsSpamContent,
                    CustomButtons = new[]
                    {
                        MainAreaTimelineResources.MsgReportAsSpamCmdReportAsSpam,
                        Resources.MsgButtonCancel
                    },
                    DefaultButtonIndex = 0,
                    AllowDialogCancellation = true,
                }));
            if (response.Response.CustomButtonResult != 0) return;
            // report as a spam
            var accounts = Setting.Accounts.Collection.ToArray();
            var reporter = accounts.FirstOrDefault();
            if (reporter == null) return;
            var rreq = new UpdateRelationRequest(User.User, RelationKind.Block);
            accounts.ToObservable()
                    .SelectMany(a =>
                        RequestQueue.EnqueueObservable(a, rreq)
                                    .Do(r => BackstageModel.RegisterEvent(
                                        new BlockedEvent(a.GetPseudoUser(), User.User))))
                    .Merge(
                        RequestQueue.EnqueueObservable(reporter,
                            new UpdateRelationRequest(User.User, RelationKind.ReportAsSpam))
                                    .Do(r =>
                                        BackstageModel.RegisterEvent(
                                            new BlockedEvent(reporter.GetPseudoUser(), User.User))))
                    .Subscribe(
                        _ => { },
                        ex => BackstageModel.RegisterEvent(new InternalErrorEvent(ex.Message)), () =>
                        {
                            var tid = Status.User.Id;
                            var tidstr = tid.ToString(CultureInfo.InvariantCulture);
                            if (Setting.MuteBlockingUsers.Value)
                            {
                                // remove statuses of blocked user
                                Task.Run(async () =>
                                {
                                    var removal = await StatusProxy.FetchStatuses(
                                        s => s.User.Id == tid ||
                                             (s.RetweetedOriginal != null && s.RetweetedOriginal.User.Id == tid),
                                        "UserId = " + tidstr + " OR BaseUserId = " + tidstr).ConfigureAwait(false);
                                    removal.ForEach(s => StatusInbox.EnqueueRemoval(s.Id));
                                });
                            }
                        });
        }

        [UsedImplicitly]
        public void MuteKeyword()
        {
            if (String.IsNullOrWhiteSpace(SelectedText))
            {
                Parent.Messenger.RaiseSafe(() =>
                    new TaskDialogMessage(new TaskDialogOptions
                    {
                        Title = MainAreaTimelineResources.MsgMuteKeywordTitle,
                        MainIcon = VistaTaskDialogIcon.Information,
                        MainInstruction = MainAreaTimelineResources.MsgMuteKeywordSelectInst,
                        Content = MainAreaTimelineResources.MsgMuteKeywordSelectContent,
                        CommonButtons = TaskDialogCommonButtons.Close,
                    }));
                return;
            }
            var response = QueryMuteMessage(MainAreaTimelineResources.MsgMuteKeywordTitle,
                MainAreaTimelineResources.MsgMuteKeywordInstFormat.SafeFormat(SelectedText),
                MainAreaTimelineResources.MsgMuteKeywordContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + Status.User.ScreenName);
            Setting.Muteds.AddPredicate(new FilterOperatorContains
            {
                LeftValue = new StatusText(),
                RightValue = new StringValue(SelectedText)
            });
        }

        public void MuteUser()
        {
            var response = QueryMuteMessage(MainAreaTimelineResources.MsgMuteUserTitle,
                MainAreaTimelineResources.MsgMuteUserInstFormat.SafeFormat("@" + Status.User.ScreenName),
                MainAreaTimelineResources.MsgMuteUserContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + Status.User.ScreenName);
            Setting.Muteds.AddPredicate(new FilterOperatorEquals
            {
                LeftValue = new UserId(),
                RightValue = new NumericValue(Status.User.Id)
            }.Or(new FilterOperatorEquals
            {
                LeftValue = new RetweeterId(),
                RightValue = new NumericValue(Status.User.Id)
            }));
        }

        public void MuteClient()
        {
            var response = QueryMuteMessage(MainAreaTimelineResources.MsgMuteClientTitle,
                MainAreaTimelineResources.MsgMuteClientInstFormat.SafeFormat("@" + SourceText),
                MainAreaTimelineResources.MsgMuteClientContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + Status.Source);
            Setting.Muteds.AddPredicate(new FilterOperatorContains
            {
                LeftValue = new StatusSource(),
                RightValue = new StringValue(SourceText)
            });
        }

        private TaskDialogMessage QueryMuteMessage(string title, string inst, string content)
        {
            return Parent.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = title,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = inst,
                    Content = content,
                    CustomButtons = new[] { MainAreaTimelineResources.MsgMuteCmdMute, Resources.MsgButtonCancel },
                    DefaultButtonIndex = 0,
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = MainAreaTimelineResources.MsgMuteFooter,
                    AllowDialogCancellation = true,
                }));
        }

        [UsedImplicitly]
        public void ReceiveOlder()
        {
            Parent.ReadMore(Status.Id);
        }

        public void OpenNthLink(string index)
        {
            int value;
            if (!int.TryParse(index, out value)) value = 0;
            var links = Status.Entities
                            .Guard()
                            .Distinct(e => e.StartIndex) // ignore extended_entities
                            .OrderBy(e => e.StartIndex)
                            .Select(e =>
                            {
                                switch (e.EntityType)
                                {
                                    case EntityType.Media:
                                    case EntityType.Urls:
                                        return e.OriginalUrl;
                                    case EntityType.UserMentions:
                                        return TextBlockStylizer.UserNavigation + e.DisplayText;
                                    case EntityType.Hashtags:
                                        return TextBlockStylizer.HashtagNavigation + e.DisplayText;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            })
                            .ToArray();
            if (value < 0 || links.Length <= value) return;
            OpenLink(links[value]);
        }

        public void OpenNthThumbnail(string index)
        {
            int value;
            if (!int.TryParse(index, out value)) value = 0;
            if (value < 0 || Images.Count <= value) return;
            Images[value].OpenImage();
        }

        #endregion

        #region OpenLinkCommand

        private ListenerCommand<string> _openLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get
            {
                return _openLinkCommand ?? (_openLinkCommand = new ListenerCommand<string>(OpenLink));
            }
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

    public class ThumbnailImageViewModel : ViewModel
    {
        private readonly Uri _source;
        private readonly Uri _display;

        public ThumbnailImageViewModel(ThumbnailImage model)
            : this(model.SourceUri, model.DisplayUri)
        {

        }

        public ThumbnailImageViewModel(Uri source, Uri display)
        {
            _source = source;
            _display = display;
        }

        public Uri SourceUri
        {
            get { return _source; }
        }

        public Uri DisplayUri
        {
            get { return _display; }
        }

        private const string TwitterImageHost = "pbs.twimg.com";

        public void OpenImage()
        {
            if (_display.Host == TwitterImageHost && Setting.OpenTwitterImageWithOriginalSize.Value)
            {
                BrowserHelper.Open(new Uri(_display.OriginalString + ":orig"));
            }
            else
            {
                BrowserHelper.Open(_source);
            }
        }
    }
}
