using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Filters;
using StarryEyes.Filters.Expressions.Operators;
using StarryEyes.Filters.Expressions.Values.Immediates;
using StarryEyes.Filters.Expressions.Values.Statuses;
using StarryEyes.Filters.Expressions.Values.Users;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Requests;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Nightmare.Windows;
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
        private readonly bool _isInReplyToExists;
        private long[] _bindingAccounts;
        private TwitterStatus _inReplyTo;
        private bool _isSelected;
        private UserViewModel _recipient;
        private UserViewModel _retweeter;
        private UserViewModel _user;
        private bool _isInReplyToLoading;
        private bool _isInReplyToLoaded;

        public StatusViewModel(StatusModel status)
            : this(null, status, null)
        {
        }

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
            this._favoritedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                this.Model.FavoritedUsers, user => new UserViewModel(user),
                DispatcherHelper.UIDispatcher, DispatcherPriority.Background);
            this.CompositeDisposable.Add(this._favoritedUsers);
            this.CompositeDisposable.Add(
                this._favoritedUsers.ListenCollectionChanged()
                               .Subscribe(_ =>
                               {
                                   this.RaisePropertyChanged(() => this.IsFavorited);
                                   this.RaisePropertyChanged(() => this.IsFavoritedUserExists);
                                   this.RaisePropertyChanged(() => this.FavoriteCount);
                               }));
            this._retweetedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                this.Model.RetweetedUsers, user => new UserViewModel(user),
                DispatcherHelper.UIDispatcher, DispatcherPriority.Background);
            this.CompositeDisposable.Add(this._retweetedUsers);
            this.CompositeDisposable.Add(
                this._retweetedUsers.ListenCollectionChanged()
                               .Subscribe(_ =>
                               {
                                   this.RaisePropertyChanged(() => this.IsRetweeted);
                                   this.RaisePropertyChanged(() => this.IsRetweetedUserExists);
                                   this.RaisePropertyChanged(() => this.RetweetCount);
                               }));
            if (this.RetweetedOriginalModel != null)
            {
                this.CompositeDisposable.Add(
                            this.RetweetedOriginalModel.FavoritedUsers.ListenCollectionChanged()
                                                  .Subscribe(_ => this.RaisePropertyChanged(() => this.IsFavorited)));
                this.CompositeDisposable.Add(
                    this.RetweetedOriginalModel.RetweetedUsers.ListenCollectionChanged()
                                          .Subscribe(_ => this.RaisePropertyChanged(() => this.IsRetweeted)));
            }

            // listen settings
            this.CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => Setting.AllowFavoriteMyself.ValueChanged += h,
                    h => Setting.AllowFavoriteMyself.ValueChanged -= h,
                    _ => this.RaisePropertyChanged(() => CanFavorite)));
            this.CompositeDisposable.Add(
                new EventListener<Action<bool>>(
                    h => Setting.ShowThumbnails.ValueChanged += h,
                    h => Setting.ShowThumbnails.ValueChanged -= h,
                    _ => this.RaisePropertyChanged(() => IsThumbnailAvailable)));
            // when account is added/removed, all timelines are regenerated.
            // so, we don't have to listen any events which notify accounts addition/deletion.

            // resolve images
            var imgsubj = this.Model.ImagesSubject;
            if (imgsubj != null)
            {
                lock (imgsubj)
                {
                    var subscribe = imgsubj
                        .Finally(() =>
                        {
                            this.RaisePropertyChanged(() => this.Images);
                            this.RaisePropertyChanged(() => this.ThumbnailImage);
                            this.RaisePropertyChanged(() => this.IsImageAvailable);
                            this.RaisePropertyChanged(() => this.IsThumbnailAvailable);
                        })
                        .Subscribe();
                    this.CompositeDisposable.Add(subscribe);
                }
            }

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
            get
            {
                return this.Model.Status.RetweetedOriginal ?? this.Model.Status;
            }
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
                       (this._user = new UserViewModel((this.Status.RetweetedOriginal ?? this.Status).User));
            }
        }

        public UserViewModel Retweeter
        {
            get { return this._retweeter ?? (this._retweeter = new UserViewModel(this.OriginalStatus.User)); }
        }

        public UserViewModel Recipient
        {
            get { return this._recipient ?? (this._recipient = new UserViewModel(this.Status.Recipient)); }
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
                if (!this.IsSourceIsLink) return this.Status.Source;
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

        public IEnumerable<Uri> Images
        {
            get { return this.Model.Images.Select(i => i.Item2); }
        }

        public bool IsThumbnailAvailable
        {
            get { return this.IsImageAvailable && Setting.ShowThumbnails.Value; }
        }

        public Uri ThumbnailImage
        {
            get { return this.Model.Images != null ? this.Model.Images.Select(i => i.Item2).FirstOrDefault() : null; }
        }

        /// <summary>
        ///     For animating helper
        /// </summary>
        internal bool IsLoaded { get; set; }

        public void RaiseFocusedChanged()
        {
            this.RaisePropertyChanged(() => this.IsFocused);
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
            if (!this.IsSourceIsLink) return;
            var start = this.Status.Source.IndexOf("\"", StringComparison.Ordinal);
            var end = this.Status.Source.IndexOf("\"", start + 1, StringComparison.Ordinal);
            if (start < 0 || end < 0) return;
            var url = this.Status.Source.Substring(start + 1, end - start - 1);
            BrowserHelper.Open(url);
        }

        private const string TwitterImageHost = "pbs.twimg.com";
        public void OpenThumbnailImage()
        {
            if (this.Model.Images == null) return;
            var tuple = this.Model.Images.FirstOrDefault();
            if (tuple == null) return;
            if (tuple.Item1.Host == TwitterImageHost && Setting.OpenTwitterImageWithOriginalSize.Value)
            {
                BrowserHelper.Open(new Uri(tuple.Item1 + ":orig"));
            }
            else
            {
                BrowserHelper.Open(tuple.Item1);
            }
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

        private void LoadInReplyTo()
        {
            if (this._isInReplyToLoading) return;
            var inReplyToStatusId = this.Status.InReplyToStatusId;
            if (inReplyToStatusId == null)
            {
                this._isInReplyToLoaded = true;
                this.RaisePropertyChanged(() => this.IsInReplyToLoaded);
                return;
            }
            this._isInReplyToLoading = true;
            this.RaisePropertyChanged(() => this.IsInReplyToLoading);
            StoreHelper.GetTweet(inReplyToStatusId.Value)
                       .Subscribe(replyTo =>
                       {
                           this._inReplyTo = replyTo;
                           this._isInReplyToLoaded = true;
                           this._isInReplyToLoading = false;
                           this.NotifyChangeReplyInfo();
                       });
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
            InputAreaModel.SetText(body: this.SelectedText);
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
            this.SetClipboard(this.Status.GetEntityAidedText(true));
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
                var msg = new TaskDialogMessage(new TaskDialogOptions
                            {
                                Title = "クリップボード エラー",
                                MainIcon = VistaTaskDialogIcon.Error,
                                MainInstruction = "コピーを行えませんでした。",
                                Content = ex.Message,
                                CommonButtons = TaskDialogCommonButtons.Close,
                            });
                this.Parent.Messenger.Raise(msg);
            }
        }

        public void Favorite(IEnumerable<TwitterAccount> infos, bool add)
        {
            Action<TwitterAccount> expected;
            Action<TwitterAccount> onFail;
            if (add)
            {
                expected = a => Task.Run(() => this.Model.AddFavoritedUser(a.Id));
                onFail = a => Task.Run(() => this.Model.RemoveFavoritedUser(a.Id));
            }
            else
            {
                expected = a => Task.Run(() => this.Model.RemoveFavoritedUser(a.Id));
                onFail = a => Task.Run(() => this.Model.AddFavoritedUser(a.Id));
            }

            var freq = new FavoriteRequest(this.Status, add);
            infos.ToObservable()
                 .Do(expected)
                 .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                 .SelectMany(a => RequestQueue.Enqueue(a, freq)
                                              .Catch((Exception ex) =>
                                              {
                                                  onFail(a);
                                                  var desc = add
                                                      ? "お気に入り登録に失敗"
                                                      : "お気に入り登録解除に失敗";
                                                  BackstageModel.RegisterEvent(new OperationFailedEvent(
                                                          desc + "(" + a.UnreliableScreenName + " -> " +
                                                          this.Status.User.ScreenName + ")", ex));
                                                  return Observable.Empty<TwitterStatus>();
                                              }))
                 .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                 .Subscribe();
        }

        public void Retweet(IEnumerable<TwitterAccount> infos, bool add)
        {
            Action<TwitterAccount> expected;
            Action<TwitterAccount> onFail;
            if (add)
            {
                expected = a => Task.Run(() => this.Model.AddRetweetedUser(a.Id));
                onFail = a => Task.Run(() => this.Model.RemoveRetweetedUser(a.Id));
            }
            else
            {
                expected = a => Task.Run(() => this.Model.RemoveRetweetedUser(a.Id));
                onFail = a => Task.Run(() => this.Model.AddRetweetedUser(a.Id));
            }
            var rreq = new RetweetRequest(this.Status, add);
            infos.ToObservable()
                 .Do(expected)
                 .Do(_ => this.RaisePropertyChanged(() => this.IsRetweeted))
                 .SelectMany(a => RequestQueue.Enqueue(a, rreq)
                                              .Catch((Exception ex) =>
                                              {
                                                  onFail(a);
                                                  var desc = add
                                                      ? "リツイートに失敗"
                                                      : "リツイート解除に失敗";
                                                  BackstageModel.RegisterEvent(new OperationFailedEvent(
                                                      desc + "(" + a.UnreliableScreenName + " -> " +
                                                      this.Status.User.ScreenName + ")", ex));
                                                  return Observable.Empty<TwitterStatus>();
                                              }))
                 .Do(_ => this.RaisePropertyChanged(() => this.IsRetweeted))
                 .Subscribe();
        }

        public void ToggleFavoriteImmediate()
        {
            if (!this.AssertQuickActionEnabled()) return;
            if (this.IsDirectMessage)
            {
                this.NotifyQuickActionFailed("このツイートはお気に入り登録できません。",
                                        "ダイレクトメッセージはお気に入り登録できません。");
                return;
            }
            if (!this.CanFavoriteImmediate && !this.IsFavorited)
            {
                this.NotifyQuickActionFailed("このツイートはお気に入り登録できません。",
                                        "自分自身のツイートをお気に入り登録しないよう設定されています。");
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
                    this.NotifyQuickActionFailed("このツイートは現在のアカウントからリツイートできません。",
                                                 "自分自身のツイートはリツイートできません。");
                }
                else
                {
                    this.NotifyQuickActionFailed("このツイートはリツイートできません。",
                                                 "非公開アカウントのツイートやダイレクトメッセージはリツイートできません。");
                }
                return;
            }
            this.Retweet(this.GetImmediateAccounts(), !this.IsRetweeted);
        }

        private bool AssertQuickActionEnabled()
        {
            if (this.BindingAccounts.Any()) return true;
            this.NotifyQuickActionFailed("アカウントが選択されていません。",
                "クイックアクションを利用するには、投稿欄横のエリアからアカウントを選択する必要があります。" + Environment.NewLine +
                "選択されているアカウントはタブごとに保持されます。");
            return false;
        }

        private void NotifyQuickActionFailed(string main, string body)
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "クイックアクション エラー",
                MainIcon = VistaTaskDialogIcon.Error,
                MainInstruction = main,
                Content = body,
                CommonButtons = TaskDialogCommonButtons.Close,
            });
            this.Parent.Messenger.Raise(msg);
        }

        public void FavoriteAndRetweetImmediate()
        {
            if (!this.AssertQuickActionEnabled()) return;
            var accounts = this.GetImmediateAccounts()
                .ToObservable()
                .Publish();
            if (!this.IsFavorited)
            {
                var freq = new FavoriteRequest(this.Status, true);
                accounts.Do(a => Task.Run(() => this.Model.AddFavoritedUser(a.Id)))
                        .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                        .SelectMany(a => RequestQueue.Enqueue(a, freq)
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
                accounts.Do(a => Task.Run(() => this.Model.AddRetweetedUser(a.Id)))
                          .Do(_ => this.RaisePropertyChanged(() => this.IsRetweeted))
                          .SelectMany(a => RequestQueue.Enqueue(a, rreq)
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
                this.NotifyQuickActionFailed("このツイートはお気に入り登録できません。",
                                        this.IsDirectMessage ? "ダイレクトメッセージはお気に入り登録できません。" :
                                        "自分自身のツイートをお気に入り登録しないよう設定されています。");
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

        public void SendReplyOrDirectMessage()
        {
            if (this.Status.StatusType == StatusType.DirectMessage)
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
            if (this.Status.StatusType == StatusType.DirectMessage)
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
            if (IsDirectMessage)
            {
                this.DirectMessage();
                return;
            }
            if (this.IsSelected)
            {
                this.Parent.ReplySelecteds();
                return;
            }
            // except my names and the user name of main target
            var excepts = this.Model.GetSuitableReplyAccount()
                              .Guard()
                              .Select(a => a.UnreliableScreenName)
                              .Append(this.User.ScreenName);
            // reply to all users
            var users = this.Status.Entities
                            .Where(e => e.EntityType == EntityType.UserMentions)
                            .Select(e => e.DisplayText)
                            .Except(excepts)
                            .Distinct()
                            .Select(s => "@" + s + " ")
                            .JoinString("");
            InputAreaModel.SetText(this.Model.GetSuitableReplyAccount(),
                "@" + this.User.ScreenName + " " + users,
                new CursorPosition(this.User.ScreenName.Length + 2, users.Length),
                this.Status);
        }

        private void Reply(string body)
        {
            if (IsDirectMessage)
            {
                this.DirectMessage(body);
                return;
            }
            // from key assign
            if (String.IsNullOrEmpty(body))
            {
                this.Reply();
                return;
            }
            try
            {
                var formatted = String.Format(body, this.User.ScreenName, this.User.Name);
                InputAreaModel.SetText(this.Model.GetSuitableReplyAccount(),
                                       formatted, inReplyTo: this.Status);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent("返信フォーマット エラー(フォーマット: " + body + ")", ex));
            }
        }

        public void Quote()
        {
            InputAreaModel.SetText(this.Model.GetSuitableReplyAccount(),
                " RT @" + this.User.ScreenName + " " + this.Status.GetEntityAidedText(true), CursorPosition.Begin);
        }

        public void QuotePermalink()
        {
            InputAreaModel.SetText(this.Model.GetSuitableReplyAccount(), " " + this.Status.Permalink, CursorPosition.Begin);
        }

        public void DirectMessage()
        {
            InputAreaModel.SetDirectMessage(this.Model.GetSuitableReplyAccount(), this.Status.User);
        }

        public void DirectMessage(string body)
        {
            var formatted = String.Format(body, this.User.ScreenName, this.User.Name);
            InputAreaModel.SetDirectMessage(this.Model.GetSuitableReplyAccount(), this.Status.User, formatted);
        }

        public void ConfirmDelete()
        {
            var footer = "直近一件のツイートの訂正は投稿欄から行えます。";
            var amendkey = KeyAssignManager.CurrentProfile
                                           .FindAssignFromActionName("Amend", KeyAssignGroup.Input)
                                           .FirstOrDefault();
            if (amendkey != null)
            {
                footer = "直近一件のツイートの訂正は、投稿欄で" + amendkey.GetKeyDescribeString() + "キーを押すと行えます。";
            }
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "ツイートの削除",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ツイートを削除しますか？",
                Content = "削除したツイートはもとに戻せません。",
                CustomButtons = new[] { "削除", "キャンセル" },
                AllowDialogCancellation = true,
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = footer,
            });
            var response = this.Parent.Messenger.GetResponse(msg);
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
                var ids = new[] { this.Status.User.Id, this.Status.Recipient.Id };
                info = ids
                    .Select(Setting.Accounts.Get).FirstOrDefault(_ => _ != null);
            }
            else
            {
                info = Setting.Accounts.Get(this.OriginalStatus.User.Id);
            }
            if (info == null) return;
            var dreq = new DeletionRequest(this.OriginalStatus);
            RequestQueue.Enqueue(info, dreq)
                .Subscribe(_ => StatusInbox.QueueRemoval(_.Id),
                    ex => BackstageModel.RegisterEvent(new OperationFailedEvent("ツイートを削除できませんでした", ex)));
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

        // this method is not supported yet.
        public void GiveFavstarTrophy()
        {
            this.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
            {
                AllowDialogCancellation = true,
                CommonButtons = TaskDialogCommonButtons.Close,
                MainIcon = VistaTaskDialogIcon.Error,
                MainInstruction = "この操作は実装されていません。",
                Content = "Favstar は オワコン",
                Title = "ツイート賞の授与",
            }));
            return;
            /*
            if (!this.AssertQuickActionEnabled()) return;
            if (Setting.FavstarApiKey.Value == null)
            {
                this.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    AllowDialogCancellation = true,
                    CommonButtons = TaskDialogCommonButtons.Close,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "この操作にはFavstar APIキーが必要です。",
                    Content = "Favstar APIキーを取得し、設定画面で登録してください。",
                    FooterIcon = VistaTaskDialogIcon.Information,
                    FooterText = "FavstarのProメンバーのみこの操作を行えます。",
                    Title = "ツイート賞の授与",
                }));
                return;
            }
            var msg = new TaskDialogMessage(new TaskDialogOptions
                        {
                            AllowDialogCancellation = true,
                            CommonButtons = TaskDialogCommonButtons.OKCancel,
                            MainIcon = VistaTaskDialogIcon.Information,
                            MainInstruction = "このツイートに今日のツイート賞を与えますか？",
                            Content = this.Status.ToString(),
                            FooterIcon = VistaTaskDialogIcon.Information,
                            FooterText = "FavstarのProメンバーのみこの操作を行えます。",
                            Title = "Favstar ツイート賞の授与",
                        });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result != TaskDialogSimpleResult.Ok) return;
            var accounts = this.GetImmediateAccounts()
                .ToObservable();
                accounts.SelectMany(a => new FavstarTrophyOperation(a, this.Status).Run())
                        .Do(_ => this.RaisePropertyChanged(() => this.IsFavorited))
                        .Subscribe();
            */
        }

        public void ReportAsSpam()
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "ユーザーをスパムとして報告",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ユーザー " + this.Status.User.ScreenName + " をスパム報告しますか？",
                Content = "全てのアカウントからブロックし、代表のアカウントからスパム報告します。",
                CustomButtons = new[] { "スパム報告", "キャンセル" },
                AllowDialogCancellation = true,
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.CustomButtonResult != 0) return;
            // report as a spam
            var accounts = Setting.Accounts.Collection.ToArray();
            var reporter = accounts.FirstOrDefault();
            if (reporter == null) return;
            var rreq = new UpdateRelationRequest(this.User.User, RelationKind.Block);
            accounts.ToObservable()
                    .SelectMany(a =>
                                RequestQueue.Enqueue(a, rreq)
                                            .Do(r => BackstageModel.RegisterEvent(
                                                new BlockedEvent(a.GetPserudoUser(), this.User.User))))
                    .Merge(
                        RequestQueue.Enqueue(reporter,
                                             new UpdateRelationRequest(this.User.User, RelationKind.ReportAsSpam))
                                    .Do(r =>
                                        BackstageModel.RegisterEvent(
                                            new BlockedEvent(reporter.GetPserudoUser(), this.User.User))))
                    .Subscribe(
                        _ => { },
                        ex => BackstageModel.RegisterEvent(new InternalErrorEvent(ex.Message)), () =>
                        {
                            var targetId = this.Status.User.Id.ToString(CultureInfo.InvariantCulture);
                            StatusProxy.FetchStatuses("UserId = " + targetId + " OR BaseUserId = " + targetId)
                                       .Subscribe(s => StatusInbox.QueueRemoval(s.Id));
                        });
        }

        public void MuteKeyword()
        {
            if (String.IsNullOrWhiteSpace(this.SelectedText))
            {
                this.Parent.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "キーワードのミュート",
                    MainIcon = VistaTaskDialogIcon.Information,
                    MainInstruction = "キーワードを選択してください。",
                    Content = "ミュートしたいキーワードをドラッグで選択できます。",
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
                return;
            }
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "キーワードのミュート",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "キーワード " + this.SelectedText + " をミュートしますか？",
                Content = "このキーワードを含むツイートが全てのタブから除外されるようになります。",
                CustomButtons = new[] { "ミュート", "キャンセル" },
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "ミュートの解除は設定画面から行えます。",
                AllowDialogCancellation = true,
            });
            var response = this.Parent.Messenger.GetResponse(msg);
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
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "ユーザーのミュート",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ユーザー " + this.Status.User.ScreenName + " をミュートしますか？",
                Content = "このユーザーのツイートが全てのタブから除外されるようになります。",
                CustomButtons = new[] { "ミュート", "キャンセル" },
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "ミュートの解除は設定画面から行えます。",
                AllowDialogCancellation = true,
            });
            var response = this.Parent.Messenger.GetResponse(msg);
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
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                Title = "クライアントのミュート",
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "クライアント " + this.SourceText + " をミュートしますか？",
                Content = "このクライアントからのツイートが全てのタブから除外されるようになります。",
                CustomButtons = new[] { "ミュート", "キャンセル" },
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "ミュートの解除は設定画面から行えます。",
                AllowDialogCancellation = true,
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.CustomButtonResult != 0) return;
            System.Diagnostics.Debug.WriteLine("Mute: " + this.Status.Source);
            Setting.Muteds.AddPredicate(new FilterOperatorContains
            {
                LeftValue = new StatusSource(),
                RightValue = new StringValue(this.SourceText)
            });
        }

        public void OpenNthLink(string index)
        {
            int value;
            if (!int.TryParse(index, out value)) value = 0;
            var links = this.Status.Entities
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
            if (links.Length <= value) return;
            this.OpenLink(links[value]);
        }

        #endregion

        #region OpenLinkCommand

        private ListenerCommand<string> _openLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get { return this._openLinkCommand ?? (this._openLinkCommand = new ListenerCommand<string>(this.OpenLink)); }
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
                    SearchFlipModel.RequestSearch("#" + param.Item2, SearchMode.Web);
                    break;
                case LinkType.Url:
                    BrowserHelper.Open(param.Item2);
                    break;
            }
        }

        #endregion
    }
}