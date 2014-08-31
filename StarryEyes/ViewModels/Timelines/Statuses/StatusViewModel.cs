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
            this._parent = parent;
            // get status model
            this.Model = status;
            this.RetweetedOriginalModel = status.RetweetedOriginal;

            // bind accounts 
            this._bindingAccounts = initialBoundAccounts.Guard().ToArray();

            // initialize users information
            this.CompositeDisposable.Add(
                this._favoritedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    this.Model.FavoritedUsers, user => new UserViewModel(user),
                    DispatcherHelper.UIDispatcher, DispatcherPriority.Background));
            this.CompositeDisposable.Add(
                this._favoritedUsers.ListenCollectionChanged(_ =>
                {
                    this.RaisePropertyChanged(() => this.IsFavorited);
                    this.RaisePropertyChanged(() => this.IsFavoritedUserExists);
                    this.RaisePropertyChanged(() => this.FavoriteCount);
                }));
            this.CompositeDisposable.Add(
                this._retweetedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                    this.Model.RetweetedUsers, user => new UserViewModel(user),
                    DispatcherHelper.UIDispatcher, DispatcherPriority.Background));
            this.CompositeDisposable.Add(
                this._retweetedUsers.ListenCollectionChanged(_ =>
                    {
                        this.RaisePropertyChanged(() => this.IsRetweeted);
                        this.RaisePropertyChanged(() => this.IsRetweetedUserExists);
                        this.RaisePropertyChanged(() => this.RetweetCount);
                    }));

            if (this.RetweetedOriginalModel != null)
            {
                this.CompositeDisposable.Add(
                    this.RetweetedOriginalModel.FavoritedUsers.ListenCollectionChanged(
                        _ => this.RaisePropertyChanged(() => this.IsFavorited)));
                this.CompositeDisposable.Add(
                    this.RetweetedOriginalModel.RetweetedUsers.ListenCollectionChanged(
                        _ => this.RaisePropertyChanged(() => this.IsRetweeted)));
            }

            // listen settings
            this.CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => Setting.AllowFavoriteMyself.ValueChanged += h,
                    h => Setting.AllowFavoriteMyself.ValueChanged -= h,
                    _ => this.RaisePropertyChanged(() => CanFavorite)));
            this.CompositeDisposable.Add(
                new EventListener<Action<ThumbnailMode>>(
                    h => Setting.ThumbnailMode.ValueChanged += h,
                    h => Setting.ThumbnailMode.ValueChanged -= h,
                    _ =>
                    {
                        this.RaisePropertyChanged(() => IsThumbnailAvailable);
                        this.RaisePropertyChanged(() => IsThumbnailsAvailable);
                    }));
            this.CompositeDisposable.Add(
                new EventListener<Action<TweetDisplayMode>>(
                    h => Setting.TweetDisplayMode.ValueChanged += h,
                    h => Setting.TweetDisplayMode.ValueChanged -= h,
                    _ =>
                    {
                        this.RaisePropertyChanged(() => IsExpanded);
                        this.RaisePropertyChanged(() => IsSingleLine);
                    }));
            // when account is added/removed, all timelines are regenerated.
            // so, we don't have to listen any events which notify accounts addition/deletion.

            this.CompositeDisposable.Add(_images = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                this.Model.Images, m => new ThumbnailImageViewModel(m), DispatcherHelper.UIDispatcher));
            // resolve images
            this.CompositeDisposable.Add(_images.ListenCollectionChanged(_ =>
            {
                this.RaisePropertyChanged(() => this.ThumbnailImage);
                this.RaisePropertyChanged(() => this.IsImageAvailable);
                this.RaisePropertyChanged(() => this.IsThumbnailAvailable);
                this.RaisePropertyChanged(() => this.IsThumbnailsAvailable);
            }));

            // look-up in-reply-to
            this._isInReplyToExists = this.Status.InReplyToStatusId.HasValue && this.Status.InReplyToStatusId != 0;
        }

        public TimelineViewModelBase Parent
        {
            get { return this._parent; }
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
            get { return this.Model.Status; }
        }

        /// <summary>
        ///     Represents status. (if this status is retweet, this property represents retweeted_original.)
        /// </summary>
        public TwitterStatus Status
        {
            get { return this.Model.Status.RetweetedOriginal ?? this.Model.Status; }
        }

        public IEnumerable<long> BindingAccounts
        {
            get { return this._bindingAccounts; }
            set
            {
                this._bindingAccounts = (value as long[]) ?? value.ToArray();
                // raise property changed
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(() => this.IsFavorited);
                this.RaisePropertyChanged(() => this.IsRetweeted);
                this.RaisePropertyChanged(() => this.IsMyselfStrict);
            }
        }

        public UserViewModel User
        {
            get
            {
                return this._user ??
                       (this._user = CreateUserViewModel((this.Status.RetweetedOriginal ?? this.Status).User));
            }
        }

        public UserViewModel Retweeter
        {
            get
            {
                if (!this.IsRetweet)
                {
                    return null;
                }
                return this._retweeter ?? (this._retweeter = CreateUserViewModel(this.OriginalStatus.User));

            }
        }

        public UserViewModel Recipient
        {
            get
            {
                if (!this.IsDirectMessage)
                {
                    return null;
                }
                return this._recipient ?? (this._recipient = CreateUserViewModel(this.Status.Recipient));
            }
        }

        private UserViewModel CreateUserViewModel(TwitterUser user)
        {
            var uvm = new UserViewModel(user);
            try
            {
                this.CompositeDisposable.Add(uvm);
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
            get { return this.Status.GetEntityAidedText(); }
        }

        public string SingleLineText
        {
            get { return this.Status.GetEntityAidedText().Replace('\n', ' ').Replace("\r", ""); }
        }

        public bool IsRetweetedUserExists
        {
            get { return this._retweetedUsers.Count > 0; }
        }

        public int RetweetCount
        {
            get { return this.RetweetedUsers.Count; }
        }

        public ReadOnlyDispatcherCollectionRx<UserViewModel> RetweetedUsers
        {
            get { return this._retweetedUsers; }
        }

        public bool IsFavoritedUserExists
        {
            get { return this._favoritedUsers.Count > 0; }
        }

        public int FavoriteCount
        {
            get { return this.FavoritedUsers.Count; }
        }

        public ReadOnlyDispatcherCollectionRx<UserViewModel> FavoritedUsers
        {
            get { return this._favoritedUsers; }
        }

        public bool IsDirectMessage
        {
            get { return this.Status.StatusType == StatusType.DirectMessage; }
        }

        public bool IsRetweet
        {
            get { return this.OriginalStatus.RetweetedOriginal != null; }
        }

        public bool IsFavorited
        {
            get
            {
                return this.RetweetedOriginalModel != null
                    ? this.RetweetedOriginalModel.IsFavorited(this._bindingAccounts)
                    : this.Model.IsFavorited(this._bindingAccounts);
            }
        }

        public bool IsRetweeted
        {
            get
            {
                return this.RetweetedOriginalModel != null
                    ? this.RetweetedOriginalModel.IsRetweeted(this._bindingAccounts)
                    : this.Model.IsRetweeted(this._bindingAccounts);
            }
        }

        public bool CanFavoriteAndRetweet
        {
            get { return this.CanFavoriteImmediate && this.CanRetweetImmediate; }
        }

        public bool CanFavorite
        {
            get { return !this.IsDirectMessage && (Setting.AllowFavoriteMyself.Value || !this.IsMyself); }
        }

        public bool CanFavoriteImmediate
        {
            get { return this.CanFavorite; }
        }

        public bool CanRetweet
        {
            get { return !this.IsDirectMessage && !this.Status.User.IsProtected; }
        }

        public bool CanRetweetImmediate
        {
            get { return this.CanRetweet && !this.IsMyselfStrict; }
        }

        public bool CanDelete
        {
            get { return this.IsDirectMessage || Setting.Accounts.Contains(this.OriginalStatus.User.Id); }
        }

        public bool IsMyself
        {
            get { return Setting.Accounts.Contains(this.OriginalStatus.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get { return this.CheckUserIsBind(this.Status.User.Id); }
        }

        private bool CheckUserIsBind(long id)
        {
            return this._bindingAccounts.Length == 1 && this._bindingAccounts[0] == id;
        }

        public bool IsInReplyToMe
        {
            get
            {
                return FilterSystemUtil.InReplyToUsers(this.Status)
                                       .Any(Setting.Accounts.Contains);
            }
        }

        public bool IsFocused
        {
            get { return this._parent.FocusedStatus == this; }
        }

        public bool IsSelected
        {
            get { return this._isSelected; }
            set
            {
                if (this._isSelected == value || this.Parent == null) return;
                this._isSelected = value;
                this.RaisePropertyChanged(() => this.IsSelected);
                this.Parent.OnSelectionUpdated();
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
            get { return this.Status.StatusType != StatusType.DirectMessage; }
        }

        public bool IsSourceIsLink
        {
            get { return this.Status.Source != null && this.Status.Source.Contains("<a href"); }
        }

        public string SourceText
        {
            get
            {
                if (this.Status.Source == null)
                {
                    return String.Empty;
                }
                if (!this.IsSourceIsLink)
                {
                    return this.Status.Source;
                }
                var start = this.Status.Source.IndexOf(">", StringComparison.Ordinal);
                var end = this.Status.Source.IndexOf("<", start + 1, StringComparison.Ordinal);
                if (start >= 0 && end >= 0)
                {
                    return this.Status.Source.Substring(start + 1, end - start - 1);
                }
                return this.Status.Source;
            }
        }

        public DateTime CreatedAt
        {
            get { return this.Status.CreatedAt; }
        }

        public bool IsImageAvailable
        {
            get { return this.Model.Images != null && this.Model.Images.Any(); }
        }

        public ReadOnlyDispatcherCollectionRx<ThumbnailImageViewModel> Images
        {
            get { return _images; }
        }

        public bool IsThumbnailAvailable
        {
            get { return this.IsImageAvailable && Setting.ThumbnailMode.Value == ThumbnailMode.Single; }
        }

        public bool IsThumbnailsAvailable
        {
            get { return this.IsImageAvailable && Setting.ThumbnailMode.Value == ThumbnailMode.All; }
        }

        public ThumbnailImageViewModel ThumbnailImage
        {
            get { return this.Model.Images != null ? this.Images.FirstOrDefault() : null; }
        }

        /// <summary>
        ///     For animating helper
        /// </summary>
        internal bool IsLoaded { get; set; }

        public void RaiseFocusedChanged()
        {
            this.RaisePropertyChanged(() => this.IsFocused);
            this.RaisePropertyChanged(() => this.IsExpanded);
            if (this.IsFocused)
            {
                this.LoadInReplyTo();
            }
        }

        public void ShowUserProfile()
        {
            SearchFlipModel.RequestSearch(this.User.ScreenName, SearchMode.UserScreenName);
        }

        public void ShowRetweeterProfile()
        {
            SearchFlipModel.RequestSearch(this.Retweeter.ScreenName, SearchMode.UserScreenName);
        }

        public void ShowRecipientProfile()
        {
            SearchFlipModel.RequestSearch(this.Recipient.ScreenName, SearchMode.UserScreenName);
        }

        public void OpenWeb()
        {
            BrowserHelper.Open(this.Status.Permalink);
        }

        public void OpenFavstar()
        {
            BrowserHelper.Open(this.Status.FavstarPermalink);
        }

        public void OpenUserDetailOnTwitter()
        {
            this.User.OpenUserDetailOnTwitter();
        }

        public void OpenUserFavstar()
        {
            this.User.OpenUserFavstar();
        }

        public void OpenUserTwilog()
        {
            this.User.OpenUserTwilog();
        }

        public void OpenSourceLink()
        {
            if (this.Status.Source == null || !this.IsSourceIsLink) return;
            var start = this.Status.Source.IndexOf("\"", StringComparison.Ordinal);
            var end = this.Status.Source.IndexOf("\"", start + 1, StringComparison.Ordinal);
            if (start < 0 || end < 0) return;
            var url = this.Status.Source.Substring(start + 1, end - start - 1);
            BrowserHelper.Open(url);
        }

        #region Reply Control

        private void NotifyChangeReplyInfo()
        {
            this.RaisePropertyChanged(() => this.IsInReplyToExists);
            this.RaisePropertyChanged(() => this.IsInReplyToLoaded);
            this.RaisePropertyChanged(() => this.IsInReplyToLoading);
            this.RaisePropertyChanged(() => this.IsInReplyToAvailable);
            this.RaisePropertyChanged(() => this.InReplyToUserImage);
            this.RaisePropertyChanged(() => this.InReplyToUserName);
            this.RaisePropertyChanged(() => this.InReplyToUserScreenName);
            this.RaisePropertyChanged(() => this.InReplyToBody);
        }

        public bool IsInReplyToExists
        {
            get { return this._isInReplyToExists; }
        }

        public bool IsInReplyToLoaded
        {
            get { return this._isInReplyToLoaded; }
        }

        public bool IsInReplyToLoading
        {
            get { return this._isInReplyToLoading; }
        }

        public bool IsInReplyToAvailable
        {
            get { return this._inReplyTo != null; }
        }

        public Uri InReplyToUserImage
        {
            get
            {
                if (this._inReplyTo == null) return null;
                return this._inReplyTo.User.ProfileImageUri;
            }
        }

        public string InReplyToUserName
        {
            get
            {
                if (this._inReplyTo == null) return null;
                return this._inReplyTo.User.Name;
            }
        }

        public string InReplyToUserScreenName
        {
            get
            {
                if (this._inReplyTo == null)
                    return this.Status.InReplyToScreenName;
                return this._inReplyTo.User.ScreenName;
            }
        }

        public string InReplyToBody
        {
            get
            {
                if (this._inReplyTo == null) return null;
                return this._inReplyTo.Text;
            }
        }

        private async void LoadInReplyTo()
        {
            if (this._isInReplyToLoading || this._isInReplyToLoaded) return;
            var inReplyToStatusId = this.Status.InReplyToStatusId;
            if (inReplyToStatusId == null)
            {
                this._isInReplyToLoaded = true;
                this.RaisePropertyChanged(() => this.IsInReplyToLoaded);
                return;
            }
            this._isInReplyToLoading = true;
            this.RaisePropertyChanged(() => this.IsInReplyToLoading);
            try
            {
                this._inReplyTo = await StoreHelper.GetTweetAsync(inReplyToStatusId.Value);
                this._isInReplyToLoaded = true;
                this._isInReplyToLoading = false;
                this.NotifyChangeReplyInfo();
            }
            catch (Exception)
            {
                this._isInReplyToLoading = false;
            }
        }

        #endregion

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
            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                Clipboard.SetText(this.SelectedText);
            }
            catch
            {
            }
            // ReSharper restore EmptyGeneralCatchClause
        }

        public void SetTextToInputBox()
        {
            InputModel.InputCore.SetText(InputSetting.Create(this.SelectedText));
        }

        public void FindOnKrile()
        {
            SearchFlipModel.RequestSearch(this.SelectedText, SearchMode.Local);
        }

        public void FindOnTwitter()
        {
            SearchFlipModel.RequestSearch(this.SelectedText, SearchMode.Web);
        }

        private const string GoogleUrl = @"http://www.google.com/search?q={0}";

        public void FindOnGoogle()
        {
            var encoded = HttpUtility.UrlEncode(this.SelectedText);
            var url = String.Format(GoogleUrl, encoded);
            BrowserHelper.Open(url);
        }

        #endregion

        #region Execution commands

        public void CopyBody()
        {
            this.SetClipboard(this.Status.GetEntityAidedText(EntityDisplayMode.LinkUri));
        }

        public void CopyPermalink()
        {
            this.SetClipboard(this.Status.Permalink);
        }

        public void CopySTOT()
        {
            this.SetClipboard(this.Status.STOTString);
        }

        private void SetClipboard(string value)
        {
            try
            {
                Clipboard.SetText(value);
            }
            catch (Exception ex)
            {
                this.Parent.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
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
                    expected = a => this.Model.AddFavoritedUser(a.GetPseudoUser());
                    onFail = a => this.Model.RemoveFavoritedUser(a.Id);
                }
                else
                {
                    expected = a => this.Model.RemoveFavoritedUser(a.Id);
                    onFail = a => this.Model.AddFavoritedUser(a.GetPseudoUser());
                }

                var request = new FavoriteRequest(this.Status, add);

                // define working task
                Func<TwitterAccount, Task> workTask = account => Task.Run(async () =>
                {
                    expected(account);
                    try
                    {
                        await RequestQueue.EnqueueAsync(account, request);
                    }
                    catch (Exception ex)
                    {
                        onFail(account);
                        var desc = add
                            ? MainAreaTimelineResources.MsgFavoriteFailed
                            : MainAreaTimelineResources.MsgUnfavoriteFailed;
                        BackstageModel.RegisterEvent(new OperationFailedEvent(
                            desc + "(" + account.UnreliableScreenName + " -> " +
                            this.Status.User.ScreenName + ")", ex));
                    }
                });

                // dispatch actions
                Task.WaitAll(infos.Select(workTask).ToArray());

                // notify changed
                this.RaisePropertyChanged(() => IsFavorited);
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
                    expected = a => this.Model.AddRetweetedUser(a.GetPseudoUser());
                    onFail = a => this.Model.RemoveRetweetedUser(a.Id);
                }
                else
                {
                    expected = a => this.Model.RemoveRetweetedUser(a.Id);
                    onFail = a => this.Model.AddRetweetedUser(a.GetPseudoUser());
                }
                var request = new RetweetRequest(this.Status, add);

                // define working task
                Func<TwitterAccount, Task> workTask = account => Task.Run(async () =>
                {
                    expected(account);
                    try
                    {
                        await RequestQueue.EnqueueAsync(account, request);
                    }
                    catch (Exception ex)
                    {
                        onFail(account);
                        var desc = add
                            ? MainAreaTimelineResources.MsgRetweetFailed
                            : MainAreaTimelineResources.MsgUnretweetFailed;
                        BackstageModel.RegisterEvent(new OperationFailedEvent(
                            desc + "(" + account.UnreliableScreenName + " -> " +
                            this.Status.User.ScreenName + ")", ex));
                    }
                });

                // dispatch actions
                Task.WaitAll(infos.Select(workTask).ToArray());

                // notify changed
                this.RaisePropertyChanged(() => this.IsRetweeted);
            });
        }

        public void ToggleFavoriteImmediate()
        {
            if (!this.AssertQuickActionEnabled()) return;
            if (this.IsDirectMessage)
            {
                this.NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    MainAreaTimelineResources.MsgProhibitFavoriteDirectMessage);
                return;
            }
            if (!this.CanFavoriteImmediate && !this.IsFavorited)
            {
                this.NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    MainAreaTimelineResources.MsgProhibitFavoriteMyself);
                return;
            }
            this.Favorite(this.GetImmediateAccounts(), !this.IsFavorited);
        }

        public void ToggleRetweetImmediate()
        {
            if (!this.AssertQuickActionEnabled()) return;
            if (!this.CanRetweetImmediate)
            {
                if (this.IsMyselfStrict)
                {
                    this.NotifyQuickActionFailed(
                        MainAreaTimelineResources.MsgProhibitRetweet,
                        MainAreaTimelineResources.MsgProhibitRetweetMyself);
                }
                else
                {
                    this.NotifyQuickActionFailed(
                        MainAreaTimelineResources.MsgProhibitRetweet,
                        MainAreaTimelineResources.MsgProhibitRetweetDirectMessage);
                }
                return;
            }
            this.Retweet(this.GetImmediateAccounts(), !this.IsRetweeted);
        }

        private bool AssertQuickActionEnabled()
        {
            if (this.BindingAccounts.Any()) return true;
            this.NotifyQuickActionFailed(
                MainAreaTimelineResources.MsgQuickActionAccountIsNotSelected,
                MainAreaTimelineResources.MsgQuickActionAccountIsNotSelectedDetail);
            return false;
        }

        private void NotifyQuickActionFailed(string main, string body)
        {
            this.Parent.Messenger.RaiseSafe(() =>
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
            if (!this.AssertQuickActionEnabled()) return;
            var accounts = this.GetImmediateAccounts()
                               .ToObservable()
                               .Publish();
            if (!this.IsFavorited)
            {
                var freq = new FavoriteRequest(this.Status, true);
                accounts.Do(a => Task.Run(() => this.Model.AddFavoritedUser(a.GetPseudoUser())))
                        .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                        .SelectMany(a => RequestQueue.EnqueueObservable(a, freq)
                                                     .Catch((Exception ex) =>
                                                     {
                                                         Task.Run(() => this.Model.RemoveFavoritedUser(a.Id));
                                                         return Observable.Empty<TwitterStatus>();
                                                     }))
                        .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                        .Subscribe();
            }
            if (!this.IsRetweeted)
            {
                var rreq = new RetweetRequest(this.Status, true);
                accounts.Do(a => Task.Run(() => this.Model.AddRetweetedUser(a.GetPseudoUser())))
                        .Do(_ => this.RaisePropertyChanged(() => this.IsRetweeted))
                        .SelectMany(a => RequestQueue.EnqueueObservable(a, rreq)
                                                     .Catch((Exception ex) =>
                                                     {
                                                         Task.Run(() => this.Model.RemoveRetweetedUser(a.Id));
                                                         return Observable.Empty<TwitterStatus>();
                                                     }))
                        .Do(_ => this.RaisePropertyChanged(() => this.IsRetweeted))
                        .Subscribe();
            }
            accounts.Connect();
        }

        private IEnumerable<TwitterAccount> GetImmediateAccounts()
        {
            return Setting.Accounts.Collection.Where(a => this._bindingAccounts.Contains(a.Id));
        }

        public void ToggleSelect()
        {
            this.IsSelected = !this.IsSelected;
        }

        public void ToggleFavorite()
        {
            if (!this.CanFavorite)
            {
                this.NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitFavorite,
                    this.IsDirectMessage
                        ? MainAreaTimelineResources.MsgProhibitFavoriteDirectMessage
                        : MainAreaTimelineResources.MsgProhibitFavoriteMyself);
                return;
            }
            var model = this.RetweetedOriginalModel ?? this.Model;
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
                    this.Favorite(adds, true);
                    this.Favorite(rmvs, false);
                });
        }

        public void ToggleRetweet()
        {
            if (!this.CanRetweet)
            {
                this.NotifyQuickActionFailed(
                    MainAreaTimelineResources.MsgProhibitRetweet,
                    this.IsDirectMessage
                        ? MainAreaTimelineResources.MsgProhibitRetweetDirectMessage
                        : MainAreaTimelineResources.MsgProhibitRetweetMyself);
                return;
            }
            var model = this.RetweetedOriginalModel ?? this.Model;
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
                    this.Retweet(adds, true);
                    this.Retweet(rmvs, false);
                });
        }

        [UsedImplicitly]
        public void SendReplyOrDirectMessage()
        {
            if (IsDirectMessage)
            {
                this.DirectMessage();
            }
            else
            {
                this.Reply();
            }
        }

        public void SendReplyOrDirectMessage(string body)
        {
            if (IsDirectMessage)
            {
                this.DirectMessage(body);
            }
            else
            {
                this.Reply(body);
            }
        }

        private void Reply()
        {
            if (this.IsSelected)
            {
                this.Parent.ReplySelecteds();
                return;
            }
            InputModel.InputCore.SetText(InputSetting.CreateReply(this.Status));
        }

        private void Reply(string body)
        {
            // from key assign
            if (String.IsNullOrEmpty(body))
            {
                this.Reply();
                return;
            }
            try
            {
                var formatted = String.Format(body, this.User.ScreenName, this.User.Name);
                InputModel.InputCore.SetText(InputSetting.CreateReply(this.Status, formatted, false));
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
            var setting = InputSetting.CreateReply(this.Status,
                " RT @" + this.User.ScreenName + ": " + this.Status.GetEntityAidedText(EntityDisplayMode.LinkUri),
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
            var setting = InputSetting.Create(this.Model.GetSuitableReplyAccount(),
                " " + this.Status.Permalink);
            setting.CursorPosition = CursorPosition.Begin;
            InputModel.InputCore.SetText(setting);
        }

        public void DirectMessage()
        {
            InputModel.InputCore.SetText(
                InputSetting.CreateDirectMessage(this.Model.GetSuitableReplyAccount(),
                    this.Status.User));
        }

        public void DirectMessage(string body)
        {
            try
            {
                var formatted = String.Format(body, this.User.ScreenName, this.User.Name);
                InputModel.InputCore.SetText(InputSetting.CreateDirectMessage(
                    this.Model.GetSuitableReplyAccount(), this.Status.User, formatted));
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
            var response = this.Parent.Messenger.GetResponseSafe(() =>
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
                this.Delete();
            }
        }

        public void Delete()
        {
            TwitterAccount info;
            if (this.IsDirectMessage)
            {
                var ids = this.Status.Recipient == null
                    ? new[] { this.Status.User.Id }
                    : new[] { this.Status.User.Id, this.Status.Recipient.Id };
                info = ids
                    .Select(Setting.Accounts.Get).FirstOrDefault(_ => _ != null);
            }
            else
            {
                info = Setting.Accounts.Get(this.OriginalStatus.User.Id);
            }
            if (info == null) return;
            Task.Run(async () =>
            {
                var dreq = new DeletionRequest(this.OriginalStatus);
                try
                {
                    var result = await RequestQueue.EnqueueAsync(info, dreq);
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
            var psel = this._lastSelectState;
            this._lastSelectState = this.IsSelected;
            if (psel != this.IsSelected) return;
            // toggle focus
            this.Parent.FocusedStatus =
                this.Parent.FocusedStatus == this ? null : this;
            if (this.Parent.FocusedStatus == this)
            {
                this.LoadInReplyTo();
            }
            this.Parent.Focus();
        }

        [UsedImplicitly]
        public void Focus()
        {
            this.Parent.FocusedStatus = this;
            this.LoadInReplyTo();
            this.Parent.Focus();
        }

        public void ShowConversation()
        {
            SearchFlipModel.RequestSearch("?from conv:\"" + this.Status.Id + "\"", SearchMode.Local);
            this.Parent.FocusedStatus = null;
        }

        public void ReportAsSpam()
        {
            var response = this.Parent.Messenger.GetResponseSafe(() =>
                new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = MainAreaTimelineResources.MsgReportAsSpamTitle,
                    MainIcon = VistaTaskDialogIcon.Warning,
                    MainInstruction = MainAreaTimelineResources.MsgReportAsSpamInstFormat
                                                               .SafeFormat("@" + this.Status.User.ScreenName),
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
            var rreq = new UpdateRelationRequest(this.User.User, RelationKind.Block);
            accounts.ToObservable()
                    .SelectMany(a =>
                        RequestQueue.EnqueueObservable(a, rreq)
                                    .Do(r => BackstageModel.RegisterEvent(
                                        new BlockedEvent(a.GetPseudoUser(), this.User.User))))
                    .Merge(
                        RequestQueue.EnqueueObservable(reporter,
                            new UpdateRelationRequest(this.User.User, RelationKind.ReportAsSpam))
                                    .Do(r =>
                                        BackstageModel.RegisterEvent(
                                            new BlockedEvent(reporter.GetPseudoUser(), this.User.User))))
                    .Subscribe(
                        _ => { },
                        ex => BackstageModel.RegisterEvent(new InternalErrorEvent(ex.Message)), () =>
                        {
                            var tid = this.Status.User.Id;
                            var tidstr = tid.ToString(CultureInfo.InvariantCulture);
                            StatusProxy.FetchStatuses(
                                s => s.User.Id == tid ||
                                     (s.RetweetedOriginal != null && s.RetweetedOriginal.User.Id == tid),
                                "UserId = " + tidstr + " OR BaseUserId = " + tidstr)
                                       .Subscribe(s => StatusInbox.EnqueueRemoval(s.Id));
                        });
        }

        [UsedImplicitly]
        public void MuteKeyword()
        {
            if (String.IsNullOrWhiteSpace(this.SelectedText))
            {
                this.Parent.Messenger.RaiseSafe(() =>
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
                MainAreaTimelineResources.MsgMuteKeywordInstFormat.SafeFormat(this.SelectedText),
                MainAreaTimelineResources.MsgMuteKeywordContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + this.Status.User.ScreenName);
            Setting.Muteds.AddPredicate(new FilterOperatorContains
            {
                LeftValue = new StatusText(),
                RightValue = new StringValue(this.SelectedText)
            });
        }

        public void MuteUser()
        {
            var response = QueryMuteMessage(MainAreaTimelineResources.MsgMuteUserTitle,
                MainAreaTimelineResources.MsgMuteUserInstFormat.SafeFormat("@" + this.Status.User.ScreenName),
                MainAreaTimelineResources.MsgMuteUserContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + this.Status.User.ScreenName);
            Setting.Muteds.AddPredicate(new FilterOperatorEquals
            {
                LeftValue = new UserId(),
                RightValue = new NumericValue(this.Status.User.Id)
            }.Or(new FilterOperatorEquals
            {
                LeftValue = new RetweeterId(),
                RightValue = new NumericValue(this.Status.User.Id)
            }));
        }

        public void MuteClient()
        {
            var response = QueryMuteMessage(MainAreaTimelineResources.MsgMuteClientTitle,
                MainAreaTimelineResources.MsgMuteClientInstFormat.SafeFormat("@" + this.SourceText),
                MainAreaTimelineResources.MsgMuteClientContent);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + this.Status.Source);
            Setting.Muteds.AddPredicate(new FilterOperatorContains
            {
                LeftValue = new StatusSource(),
                RightValue = new StringValue(this.SourceText)
            });
        }

        private TaskDialogMessage QueryMuteMessage(string title, string inst, string content)
        {
            return this.Parent.Messenger.GetResponseSafe(() =>
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
            this.Parent.ReadMore(this.Status.Id);
        }

        public void OpenNthLink(string index)
        {
            int value;
            if (!int.TryParse(index, out value)) value = 0;
            var links = this.Status.Entities
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
            this.OpenLink(links[value]);
        }

        public void OpenNthThumbnail(string index)
        {
            int value;
            if (!int.TryParse(index, out value)) value = 0;
            if (value < 0 || this.Images.Count <= value) return;
            this.Images[value].OpenImage();
        }

        #endregion

        #region OpenLinkCommand

        private ListenerCommand<string> _openLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get
            {
                return this._openLinkCommand ?? (this._openLinkCommand = new ListenerCommand<string>(this.OpenLink));
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
            this._source = source;
            this._display = display;
        }

        public Uri SourceUri
        {
            get { return this._source; }
        }

        public Uri DisplayUri
        {
            get { return this._display; }
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
