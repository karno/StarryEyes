using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Livet;
using Livet.Commands;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;
using StarryEyes.Views.Utils;
using TaskDialogInterop;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class StatusViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<UserViewModel> _favoritedUsers;
        private readonly TimelineViewModelBase _parent;
        private readonly ReadOnlyDispatcherCollectionRx<UserViewModel> _retweetedUsers;
        private long[] _bindingAccounts;
        private TwitterStatus _inReplyTo;
        private bool _isSelected;
        private UserViewModel _recipient;
        private UserViewModel _retweeter;
        private UserViewModel _user;

        public StatusViewModel(TwitterStatus status)
            : this(null, status, null)
        {
        }

        public StatusViewModel(TimelineViewModelBase parent, TwitterStatus status,
                               IEnumerable<long> initialBoundAccounts)
        {
            _parent = parent;
            // get status model
            Model = StatusModel.Get(status);

            // bind accounts 
            _bindingAccounts = initialBoundAccounts.Guard().ToArray();

            // initialize users information
            _favoritedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                Model.FavoritedUsers, user => new UserViewModel(user), DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_favoritedUsers);
            CompositeDisposable.Add(
                _favoritedUsers.ListenCollectionChanged()
                               .Subscribe(_ =>
                               {
                                   RaisePropertyChanged(() => IsFavoritedUserExists);
                                   RaisePropertyChanged(() => FavoriteCount);
                               }));
            _retweetedUsers = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                Model.RetweetedUsers, user => new UserViewModel(user), DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_retweetedUsers);
            CompositeDisposable.Add(
                _retweetedUsers.ListenCollectionChanged()
                               .Subscribe(_ =>
                               {
                                   RaisePropertyChanged(() => IsRetweetedUserExists);
                                   RaisePropertyChanged(() => RetweetCount);
                               }));

            // resolve images
            var imgsubj = Model.ImagesSubject;
            if (imgsubj != null)
            {
                lock (imgsubj)
                {
                    var subscribe = imgsubj
                        .Finally(() =>
                        {
                            RaisePropertyChanged(() => Images);
                            RaisePropertyChanged(() => FirstImage);
                            RaisePropertyChanged(() => IsImageAvailable);
                        })
                        .Subscribe();
                    CompositeDisposable.Add(subscribe);
                }
            }

            // look-up in-reply-to
            if (status.InReplyToStatusId.HasValue)
            {
                var subscribe = StoreHub.GetTweet(status.InReplyToStatusId.Value)
                        .Subscribe(replyTo =>
                        {
                            _inReplyTo = replyTo;
                            RaisePropertyChanged(() => IsInReplyToExists);
                            RaisePropertyChanged(() => InReplyToUserImage);
                            RaisePropertyChanged(() => InReplyToUserName);
                            RaisePropertyChanged(() => InReplyToUserScreenName);
                            RaisePropertyChanged(() => InReplyToBody);
                        });
                CompositeDisposable.Add(subscribe);
            }
        }

        public TimelineViewModelBase Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///     Represents status model.
        /// </summary>
        public StatusModel Model { get; private set; }

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
            get
            {
                if (Model.Status.RetweetedOriginal != null)
                    return Model.Status.RetweetedOriginal;
                return Model.Status;
            }
        }

        public IEnumerable<long> BindingAccounts
        {
            get { return _bindingAccounts; }
            set
            {
                _bindingAccounts = value.ToArray();
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
                       (_user = new UserViewModel((Status.RetweetedOriginal ?? Status).User));
            }
        }

        public UserViewModel Retweeter
        {
            get { return _retweeter ?? (_retweeter = new UserViewModel(OriginalStatus.User)); }
        }

        public UserViewModel Recipient
        {
            get { return _recipient ?? (_recipient = new UserViewModel(Status.Recipient)); }
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
            get { return Model.IsFavorited(_bindingAccounts); }
        }

        public bool IsRetweeted
        {
            get { return Model.IsRetweeted(_bindingAccounts); }
        }

        public bool CanFavoriteAndRetweet
        {
            get { return CanFavoriteImmediate && CanRetweetImmediate; }
        }

        public bool CanFavorite
        {
            get { return !IsDirectMessage; }
        }

        public bool CanFavoriteImmediate
        {
            get { return CanFavorite && (Setting.AllowFavoriteMyself.Value || !IsMyselfStrict); }
        }

        public bool CanRetweet
        {
            get { return !IsDirectMessage && !Status.User.IsProtected; }
        }

        public bool CanRetweetImmediate
        {
            get { return CanRetweet && !IsMyselfStrict; }
        }

        public bool CanDelete
        {
            get { return IsDirectMessage || AccountsStore.AccountIds.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyself
        {
            get { return AccountsStore.AccountIds.Contains(OriginalStatus.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get { return _bindingAccounts.Length == 1 && _bindingAccounts[0] == Status.User.Id; }
        }

        public bool IsInReplyToExists
        {
            get { return _inReplyTo != null; }
        }

        public bool IsInReplyToMe
        {
            get
            {
                return FilterSystemUtil.InReplyToUsers(Status)
                                       .Any(AccountsStore.AccountIds.Contains);
            }
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

        public bool IsFocused
        {
            get { return _parent.FocusedStatus == this; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value && Parent != null)
                {
                    _isSelected = value;
                    RaisePropertyChanged(() => IsSelected);
                    Parent.OnSelectionUpdated();
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
                if (IsSourceIsLink)
                {
                    int start = Status.Source.IndexOf(">", StringComparison.Ordinal);
                    int end = Status.Source.IndexOf("<", start + 1, StringComparison.Ordinal);
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

        public IEnumerable<Uri> Images
        {
            get { return Model.Images.Select(i => i.Item2); }
        }

        public Uri FirstImage
        {
            get { return Model.Images != null ? Model.Images.Select(i => i.Item2).FirstOrDefault() : null; }
        }

        /// <summary>
        ///     For animating helper
        /// </summary>
        internal bool IsLoaded { get; set; }

        public void RaiseFocusedChanged()
        {
            RaisePropertyChanged(() => IsFocused);
            if (IsFocused)
            {
                Messenger.Raise(new BringIntoViewMessage());
            }
        }

        public void ShowUserProfile()
        {

        }

        public void OpenRetweeterProfile()
        {

        }

        public void OpenWeb()
        {
            BrowserHelper.Open(Status.Permalink);
        }

        public void OpenFavstar()
        {
            BrowserHelper.Open(Status.FavstarPermalink);
        }

        public void OpenUserWeb()
        {
            BrowserHelper.Open(Status.UserPermalink);
        }

        public void OpenUserFavstar()
        {
            BrowserHelper.Open(Status.FavstarUserPermalink);
        }

        public void OpenUserTwilog()
        {
            BrowserHelper.Open(Status.TwilogUserPermalink);
        }

        public void OpenSourceLink()
        {
            if (!IsSourceIsLink) return;
            int start = Status.Source.IndexOf("\"", StringComparison.Ordinal);
            int end = Status.Source.IndexOf("\"", start + 1, StringComparison.Ordinal);
            string url = Status.Source.Substring(start + 1, end - start - 1);
            BrowserHelper.Open(url);
        }

        public void OpenFirstImage()
        {
            if (Model.Images == null) return;
            Tuple<Uri, Uri> tuple = Model.Images.FirstOrDefault();
            if (tuple == null) return;
            BrowserHelper.Open(tuple.Item1);
        }

        #region Execution commands

        public void CopyBody()
        {
            SetClipboard(Status.GetEntityAidedText(true));
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
                var msg = new TaskDialogMessage(new TaskDialogOptions
                            {
                                CommonButtons = TaskDialogCommonButtons.Close,
                                MainIcon = VistaTaskDialogIcon.Error,
                                MainInstruction = "コピーを行えませんでした。",
                                Content = ex.Message,
                                Title = "クリップボード エラー"
                            });
                this.Parent.Messenger.Raise(msg);
            }
        }

        public void Favorite(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected;
            Action<AuthenticateInfo> onFail;
            if (add)
            {
                expected = a => Model.AddFavoritedUser(a.Id);
                onFail = a => Model.RemoveFavoritedUser(a.Id);
            }
            else
            {
                expected = a => Model.RemoveFavoritedUser(a.Id);
                onFail = a => Model.AddFavoritedUser(a.Id);
            }

            infos.ToObservable()
                 .Do(expected)
                 .Do(_ => RaisePropertyChanged(() => IsFavorited))
                 .SelectMany(a => new FavoriteOperation(a, Status, add)
                                      .Run()
                                      .Catch((Exception ex) =>
                                      {
                                          onFail(a);
                                          BackpanelModel.RegisterEvent(
                                              new OperationFailedEvent((add ? "" : "un") + "favorite failed: " +
                                                                       a.UnreliableScreenName + " -> " +
                                                                       Status.User.ScreenName + " :" +
                                                                       ex.Message));
                                          return Observable.Empty<TwitterStatus>();
                                      }))
                 .Do(_ => RaisePropertyChanged(() => IsFavorited))
                 .Subscribe();
        }

        public void Retweet(IEnumerable<AuthenticateInfo> infos, bool add)
        {
            Action<AuthenticateInfo> expected;
            Action<AuthenticateInfo> onFail;
            if (add)
            {
                expected = a => Model.AddRetweetedUser(a.Id);
                onFail = a => Model.RemoveRetweetedUser(a.Id);
            }
            else
            {
                expected = a => Model.RemoveRetweetedUser(a.Id);
                onFail = a => Model.AddRetweetedUser(a.Id);
            }
            infos.ToObservable()
                 .Do(expected)
                 .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                 .SelectMany(a => new RetweetOperation(a, Status, add)
                                      .Run()
                                      .Catch((Exception ex) =>
                                      {
                                          onFail(a);
                                          BackpanelModel.RegisterEvent(
                                              new OperationFailedEvent((add ? "" : "un") + "retweet failed: " +
                                                                       a.UnreliableScreenName + " -> " +
                                                                       Status.User.ScreenName + " :" +
                                                                       ex.Message));
                                          return Observable.Empty<TwitterStatus>();
                                      }))
                 .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                 .Subscribe();
        }

        public void ToggleFavoriteImmediate()
        {
            if (!AssertQuickActionEnabled()) return;
            if (!CanFavoriteImmediate && !IsFavorited)
            {
                NotifyQuickActionFailed("このツイートは現在のアカウントからお気に入り登録できません。",
                    "Krile上で自分自身のツイートをお気に入り登録しないよう設定されています。");
                return;
            }
            Favorite(GetImmediateAccounts(), !IsFavorited);
        }

        public void ToggleRetweetImmediate()
        {
            if (!AssertQuickActionEnabled()) return;
            if (!CanRetweetImmediate)
            {
                NotifyQuickActionFailed("このツイートは現在のアカウントからリツイートできません。",
                    "自分自身のツイートはリツイートできません。");
                return;
            }
            Retweet(GetImmediateAccounts(), !IsRetweeted);
        }

        private bool AssertQuickActionEnabled()
        {
            if (!BindingAccounts.Any())
            {
                NotifyQuickActionFailed("アカウントが選択されていません。",
                                        "クイックアクションを利用するには、投稿欄横のエリアからアカウントを選択する必要があります。" + Environment.NewLine +
                                        "選択されているアカウントはタブごとに保持されます。");
                return false;
            }
            return true;
        }

        private void NotifyQuickActionFailed(string main, string body)
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                CommonButtons = TaskDialogCommonButtons.Close,
                MainIcon = VistaTaskDialogIcon.Error,
                MainInstruction = main,
                Content = body,
                Title = "クイックアクション エラー"
            });
            this.Parent.Messenger.Raise(msg);
        }

        public void FavoriteAndRetweetImmediate()
        {
            if (!AssertQuickActionEnabled()) return;
            var accounts = GetImmediateAccounts()
                .ToObservable()
                .Publish();
            if (!IsFavorited)
                accounts.Do(a => Model.AddFavoritedUser(a.Id))
                        .Do(_ => RaisePropertyChanged(() => IsFavorited))
                        .SelectMany(a => new FavoriteOperation(a, Status, true)
                                             .Run()
                                             .Catch((Exception ex) =>
                                             {
                                                 Model.RemoveFavoritedUser(a.Id);
                                                 return Observable.Empty<TwitterStatus>();
                                             }))
                        .Do(_ => RaisePropertyChanged(() => IsFavorited))
                        .Subscribe();
            if (!IsRetweeted)
                accounts.Do(a => Model.AddRetweetedUser(a.Id))
                        .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                        .SelectMany(a => new RetweetOperation(a, Status, true)
                                             .Run()
                                             .Catch((Exception ex) =>
                                             {
                                                 Model.RemoveRetweetedUser(a.Id);
                                                 return Observable.Empty<TwitterStatus>();
                                             }))
                        .Do(_ => RaisePropertyChanged(() => IsRetweeted))
                        .Subscribe();
            accounts.Connect();
        }

        private IEnumerable<AuthenticateInfo> GetImmediateAccounts()
        {
            return AccountsStore.Accounts
                                .Where(a => _bindingAccounts.Contains(a.UserId))
                                .Select(a => a.AuthenticateInfo);
        }

        public void ToggleSelect()
        {
            IsSelected = !IsSelected;
        }

        public void ToggleFavorite()
        {
            AuthenticateInfo[] favoriteds =
                AccountsStore.Accounts
                             .Where(a => Model.IsFavorited(a.UserId))
                             .Select(a => a.AuthenticateInfo)
                             .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(
                AccountSelectionAction.Favorite,
                Status,
                favoriteds,
                infos =>
                {
                    AuthenticateInfo[] authenticateInfos =
                        infos as AuthenticateInfo[] ?? infos.ToArray();
                    IEnumerable<AuthenticateInfo> adds =
                        authenticateInfos.Except(favoriteds);
                    IEnumerable<AuthenticateInfo> rmvs =
                        favoriteds.Except(authenticateInfos);
                    Favorite(adds, true);
                    Favorite(rmvs, false);
                });
        }

        public void ToggleRetweet()
        {
            AuthenticateInfo[] retweeteds =
                AccountsStore.Accounts
                             .Where(a => Model.IsRetweeted(a.UserId))
                             .Select(a => a.AuthenticateInfo)
                             .ToArray();
            MainWindowModel.ExecuteAccountSelectAction(
                AccountSelectionAction.Retweet,
                Status,
                retweeteds,
                infos =>
                {
                    AuthenticateInfo[] authenticateInfos =
                        infos as AuthenticateInfo[] ?? infos.ToArray();
                    IEnumerable<AuthenticateInfo> adds =
                        authenticateInfos.Except(retweeteds);
                    IEnumerable<AuthenticateInfo> rmvs =
                        retweeteds.Except(authenticateInfos);
                    Retweet(adds, true);
                    Retweet(rmvs, false);
                });
        }

        public void SendReply()
        {
            if (Status.StatusType == StatusType.DirectMessage)
            {
                DirectMessage();
            }
            else
            {
                Reply();
            }
        }

        public void Reply()
        {
            if (IsSelected)
            {
                Parent.ReplySelecteds();
            }
            else
            {
                InputAreaModel.SetText(Model.GetSuitableReplyAccount(), "@" + User.ScreenName + " ", inReplyTo: Status);
            }
        }

        public void Quote()
        {
            InputAreaModel.SetText(Model.GetSuitableReplyAccount(),
                " RT @" + User.ScreenName + " " + Status.GetEntityAidedText(true), CursorPosition.Begin);
        }

        public void QuotePermalink()
        {
            InputAreaModel.SetText(Model.GetSuitableReplyAccount(), " " + Status.Permalink, CursorPosition.Begin);
        }

        public void DirectMessage()
        {
            InputAreaModel.SetDirectMessage(Model.GetSuitableReplyAccount(), Status.User);
        }

        public void ConfirmDelete()
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                AllowDialogCancellation = true,
                CommonButtons = TaskDialogCommonButtons.OKCancel,
                Content = "削除したツイートはもとに戻せません。",
                FooterIcon = VistaTaskDialogIcon.Information,
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ツイートを削除しますか？",
                FooterText = "直近一件のツイートの訂正は、投稿欄で↑キーを押すと行えます。",
                Title = "ツイートの削除",
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result == TaskDialogSimpleResult.Ok)
            {
                Delete();
            }
        }

        public void Delete()
        {
            AuthenticateInfo info = null;
            if (IsDirectMessage)
            {
                var ids = new[] { Status.User.Id, Status.Recipient.Id };
                info = ids
                    .Select(AccountsStore.GetAccountSetting)
                    .Where(_ => _ != null)
                    .Select(_ => _.AuthenticateInfo)
                    .FirstOrDefault();
            }
            else
            {
                AccountSetting ai = AccountsStore.GetAccountSetting(OriginalStatus.User.Id);
                if (ai != null)
                {
                    info = ai.AuthenticateInfo;
                }
            }
            if (info != null)
            {
                new DeleteOperation(info, OriginalStatus)
                    .Run()
                    .Subscribe(_ => StatusStore.Remove(_.Id),
                               ex => AppInformationHub.PublishInformation(
                                   new AppInformation(AppInformationKind.Error,
                                                      "ERR_DELETE_MSG_" +
                                                      Status.Id,
                                                      "ステータスを削除できませんでした。",
                                                      ex.Message,
                                                      "再試行", Delete)));
            }
        }

        private bool _lastSelectState;
        public void ToggleFocus()
        {
            var psel = _lastSelectState;
            _lastSelectState = IsSelected;
            if (psel != IsSelected) return;
            Parent.FocusedStatus =
                Parent.FocusedStatus == this ? null : this;
        }

        public void ShowConversation()
        {
            // TODO: Implementation
        }

        public void GiveFavstarTrophy()
        {
            if (!AssertQuickActionEnabled()) return;
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
                            Content = Status.ToString(),
                            FooterIcon = VistaTaskDialogIcon.Information,
                            FooterText = "FavstarのProメンバーのみこの操作を行えます。",
                            Title = "Favstar ツイート賞の授与",
                        });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result == TaskDialogSimpleResult.Ok)
            {
                var accounts = GetImmediateAccounts()
                    .ToObservable();
                accounts.SelectMany(a => new FavstarTrophyOperation(a, this.Status).Run())
                        .Do(_ => RaisePropertyChanged(() => IsFavorited))
                        .Subscribe();
            }
        }

        public void ReportAsSpam()
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                AllowDialogCancellation = true,
                CommonButtons = TaskDialogCommonButtons.OKCancel,
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ユーザー " + Status.User.ScreenName + " をスパム報告しますか？",
                Content = "全てのアカウントからブロックし、代表のアカウントからスパム報告します。",
                Title = "ユーザーをスパムとして報告",
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result == TaskDialogSimpleResult.Ok)
            {
                // report as a spam
                // TODO
                System.Diagnostics.Debug.WriteLine("R4S: " + Status.User.ScreenName);
            }
        }

        public void MuteKeyword()
        {
            // TODO
        }

        public void MuteUser()
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                AllowDialogCancellation = true,
                CommonButtons = TaskDialogCommonButtons.OKCancel,
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "ユーザー " + Status.User.ScreenName + " をミュートしますか？",
                Content = "このユーザーのツイートが全てのタブから除外されるようになります。",
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "ミュートの解除は設定画面から行えます。",
                Title = "ユーザーのミュート",
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result == TaskDialogSimpleResult.Ok)
            {
                // report as a spam
                // TODO
                System.Diagnostics.Debug.WriteLine("Mute: " + Status.User.ScreenName);
            }
        }

        public void MuteClient()
        {
            var msg = new TaskDialogMessage(new TaskDialogOptions
            {
                AllowDialogCancellation = true,
                CommonButtons = TaskDialogCommonButtons.OKCancel,
                MainIcon = VistaTaskDialogIcon.Warning,
                MainInstruction = "クライアント " + SourceText + " をスパム報告しますか？",
                Content = "このクライアントからのツイートが全てのタブから除外されるようになります。",
                FooterIcon = VistaTaskDialogIcon.Information,
                FooterText = "ミュートの解除は設定画面から行えます。",
                Title = "クライアントのミュート",
            });
            var response = this.Parent.Messenger.GetResponse(msg);
            if (response.Response.Result == TaskDialogSimpleResult.Ok)
            {
                // report as a spam
                System.Diagnostics.Debug.WriteLine("Mute: " + Status.Source);
            }
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
            Tuple<LinkType, string> param = StatusStylizer.ResolveInternalUrl(parameter);
            switch (param.Item1)
            {
                case LinkType.User:
                    BrowserHelper.Open("http://twitter.com/" + param.Item2);
                    break;
                case LinkType.Hash:
                    BrowserHelper.Open("http://twitter.com/search/?q=" + param.Item2);
                    break;
                case LinkType.Url:
                    BrowserHelper.Open(param.Item2);
                    break;
            }
        }

        #endregion
    }
}