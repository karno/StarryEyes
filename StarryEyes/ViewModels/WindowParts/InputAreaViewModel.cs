using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.NotificationEvents.PostEvents;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Flips;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Views.Messaging;
using TaskDialogInterop;

namespace StarryEyes.ViewModels.WindowParts
{
    public class InputAreaViewModel : ViewModel
    {
        private readonly AccountSelectionFlipViewModel _accountSelectionFlip;
        private readonly DispatcherCollection<BindHashtagViewModel> _bindableHashtagCandidates;
        private readonly ReadOnlyDispatcherCollectionRx<AuthenticateInfoViewModel> _bindingAuthInfos;

        private readonly ReadOnlyDispatcherCollectionRx<BindHashtagViewModel> _bindingHashtags;
        private readonly ReadOnlyDispatcherCollectionRx<TweetInputInfoViewModel> _draftedInputs;
        private readonly GeoCoordinateWatcher _geoWatcher;
        private long[] _baseSelectedAccounts;
        private UserViewModel _directMessageToCache;
        private StatusViewModel _inReplyToViewModelCache;
        private TweetInputInfo _inputInfo;
        private bool _isLocationEnabled;
        private bool _isOpening;
        private bool _isSuppressAccountChangeRelay;

        /// <summary>
        ///     Constructor
        /// </summary>
        public InputAreaViewModel()
        {
            _accountSelectionFlip = new AccountSelectionFlipViewModel();
            _accountSelectionFlip.OnClosed += () =>
            {
                // After selection accounts, return focus to text box
                // if input area is opened.
                if (IsOpening)
                {
                    FocusToTextBox();
                }
            };

            CompositeDisposable.Add(_bindingHashtags =
                                    ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                                        InputAreaModel.BindingHashtags,
                                        _ => new BindHashtagViewModel(_, () => UnbindHashtag(_)),
                                        DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(_bindingHashtags
                                        .ListenCollectionChanged()
                                        .Subscribe(_ => RaisePropertyChanged(() => IsBindingHashtagExisted)));

            _bindableHashtagCandidates =
                new DispatcherCollection<BindHashtagViewModel>(DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_bindableHashtagCandidates
                                        .ListenCollectionChanged()
                                        .Subscribe(_ => RaisePropertyChanged(() => IsBindableHashtagExisted)));

            CompositeDisposable.Add(_draftedInputs =
                                    ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                                        InputAreaModel.Drafts,
                                        _ =>
                                        new TweetInputInfoViewModel(this, _, vm => InputAreaModel.Drafts.Remove(vm)),
                                        DispatcherHelper.UIDispatcher));

            CompositeDisposable.Add(_draftedInputs
                                        .ListenCollectionChanged()
                                        .Subscribe(_ =>
                                        {
                                            RaisePropertyChanged(() => DraftCount);
                                            RaisePropertyChanged(() => IsDraftsExisted);
                                        }));

            CompositeDisposable.Add(_bindingAuthInfos =
                                    ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                                        InputAreaModel.BindingAuthInfos,
                                        _ => new AuthenticateInfoViewModel(_),
                                        DispatcherHelper.UIDispatcher));

            CompositeDisposable.Add(_bindingAuthInfos
                                        .ListenCollectionChanged()
                                        .Subscribe(_ => RaisePropertyChanged(() => IsBindingAuthInfoExisted)));

            bool accountSelectReflecting = false;
            _accountSelectionFlip.OnSelectedAccountsChanged += () =>
            {
                if (!_isSuppressAccountChangeRelay)
                {
                    // write-back
                    accountSelectReflecting = true;
                    InputAreaModel.BindingAuthInfos.Clear();
                    _accountSelectionFlip.SelectedAccounts
                                    .ForEach(InputAreaModel.BindingAuthInfos.Add);
                    accountSelectReflecting = false;
                    _baseSelectedAccounts = InputAreaModel.BindingAuthInfos.Select(_ => _.Id).ToArray();
                }
                InputInfo.AuthInfos = AccountSelectionFlip.SelectedAccounts;
                RaisePropertyChanged(() => AuthInfoGridRowColumn);
                UpdateTextCount();
                RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
            };
            CompositeDisposable.Add(_accountSelectionFlip);
            CompositeDisposable.Add(
                new CollectionChangedEventListener(
                    InputAreaModel.BindingAuthInfos,
                    (_, __) =>
                    {
                        RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
                        if (accountSelectReflecting) return;
                        _baseSelectedAccounts = InputAreaModel.BindingAuthInfos
                                                              .Select(a => a.Id)
                                                              .ToArray();
                        ApplyBaseSelectedAccounts();
                        UpdateTextCount();
                    }));

            CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus>>(
                    _ => InputAreaModel.OnSetTextRequested += _,
                    _ => InputAreaModel.OnSetTextRequested -= _,
                    (infos, body, cursor, inReplyTo) =>
                    {
                        OpenInput(false);
                        if (!CheckClearInput(body)) return;
                        if (infos != null)
                        {
                            OverrideSelectedAccounts(infos);
                        }
                        if (inReplyTo != null)
                        {
                            InReplyTo = new StatusViewModel(inReplyTo);
                        }
                        switch (cursor)
                        {
                            case CursorPosition.Begin:
                                Messenger.RaiseAsync(new TextBoxSetCaretMessage(0));
                                break;
                            case CursorPosition.End:
                                Messenger.RaiseAsync(new TextBoxSetCaretMessage(InputText.Length));
                                break;
                        }
                    }));

            CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<AuthenticateInfo>, TwitterUser>>(
                    _ => InputAreaModel.OnSendDirectMessageRequested += _,
                    _ => InputAreaModel.OnSendDirectMessageRequested -= _,
                    (infos, user) =>
                    {
                        OpenInput(false);
                        CheckClearInput();
                        OverrideSelectedAccounts(infos);
                        DirectMessageTo = new UserViewModel(user);
                    }));

            _geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            _geoWatcher.StatusChanged += (_, e) =>
            {
                if (e.Status != GeoPositionStatus.Ready)
                {
                    IsLocationEnabled = true;
                }
                else
                {
                    IsLocationEnabled = false;
                    AttachedLocation = null;
                }
            };
            CompositeDisposable.Add(_geoWatcher);
            _geoWatcher.Start();
        }

        public AccountSelectionFlipViewModel AccountSelectionFlip
        {
            get { return _accountSelectionFlip; }
        }

        public ReadOnlyDispatcherCollectionRx<BindHashtagViewModel> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        public bool IsBindingHashtagExisted
        {
            get { return _bindingHashtags != null && _bindingHashtags.Count > 0; }
        }

        public DispatcherCollection<BindHashtagViewModel> BindableHashtagCandidates
        {
            get { return _bindableHashtagCandidates; }
        }

        public bool IsBindableHashtagExisted
        {
            get { return _bindableHashtagCandidates != null && _bindableHashtagCandidates.Count > 0; }
        }

        public ReadOnlyDispatcherCollectionRx<TweetInputInfoViewModel> DraftedInputs
        {
            get { return _draftedInputs; }
        }

        public bool IsBindingAuthInfoExisted
        {
            get { return _bindingAuthInfos != null && _bindingAuthInfos.Count > 0; }
        }

        public ReadOnlyDispatcherCollectionRx<AuthenticateInfoViewModel> BindingAuthInfos
        {
            get { return _bindingAuthInfos; }
        }

        public int AuthInfoGridRowColumn
        {
            get { return (int)Math.Ceiling(Math.Sqrt(Math.Max(_bindingAuthInfos.Count, 1))); }
        }

        public string AuthInfoScreenNames
        {
            get
            {
                if (_bindingAuthInfos.Count == 0)
                    return "アカウントは選択されていません。";
                return _bindingAuthInfos.Select(_ => _.ScreenName).JoinString(", ") + "が選択されています。";
            }
        }

        public bool IsOpening
        {
            get { return _isOpening; }
            set
            {
                if (_isOpening == value) return;
                _isOpening = value;
                RaisePropertyChanged(() => IsOpening);
                Messenger.RaiseAsync(value ? new GoToStateMessage("Open") : new GoToStateMessage("Close"));
            }
        }

        public TweetInputInfo InputInfo
        {
            get
            {
                if (_inputInfo == null)
                    ClearInput();
                return _inputInfo;
            }
            set
            {
                if (CanSaveToDraft)
                {
                    InputAreaModel.Drafts.Add(InputInfo);
                }
                _inputInfo = value;
                OverrideSelectedAccounts(value.AuthInfos);
                RaisePropertyChanged(() => InputInfo);
                RaisePropertyChanged(() => InputText);
                RaisePropertyChanged(() => InReplyTo);
                RaisePropertyChanged(() => IsInReplyToEnabled);
                RaisePropertyChanged(() => DirectMessageTo);
                RaisePropertyChanged(() => IsDirectMessageEnabled);
                RaisePropertyChanged(() => AttachedImage);
                RaisePropertyChanged(() => IsImageAttached);
                RaisePropertyChanged(() => AttachedLocation);
                RaisePropertyChanged(() => IsLocationAttached);
                UpdateHashtagCandidates();
                UpdateTextCount();
            }
        }

        public string InputText
        {
            get { return InputInfo.Text ?? String.Empty; }
            set
            {
                InputInfo.Text = value;
                RaisePropertyChanged(() => InputText);
                UpdateHashtagCandidates();
                UpdateTextCount();
                if (IsUrlAutoEsacpeEnabled)
                    EscapeUrl();
            }
        }

        public bool IsUrlAutoEsacpeEnabled
        {
            get { return Setting.IsUrlAutoEscapeEnabled.Value; }
            set
            {
                Setting.IsUrlAutoEscapeEnabled.Value = value;
                RaisePropertyChanged(() => IsUrlAutoEsacpeEnabled);
                if (value)
                    EscapeUrl();
            }
        }

        public StatusViewModel InReplyTo
        {
            get
            {
                if (InputInfo.InReplyTo == null)
                {
                    return null;
                }

                if (_inReplyToViewModelCache == null ||
                    _inReplyToViewModelCache.Status.Id != InputInfo.InReplyTo.Id)
                {
                    _inReplyToViewModelCache = new StatusViewModel(InputInfo.InReplyTo);
                }
                return _inReplyToViewModelCache;
            }
            set
            {
                if (_inReplyToViewModelCache != null)
                {
                    _inReplyToViewModelCache.Dispose();
                }
                if (value == null)
                {
                    _inReplyToViewModelCache = null;
                    InputInfo.InReplyTo = null;
                }
                else
                {
                    _inReplyToViewModelCache = value;
                    InputInfo.InReplyTo = value.Status;
                }
                RaisePropertyChanged(() => InReplyTo);
                RaisePropertyChanged(() => IsInReplyToEnabled);
            }
        }

        public bool IsInReplyToEnabled
        {
            get { return InputInfo.InReplyTo != null; }
        }

        public UserViewModel DirectMessageTo
        {
            get
            {
                if (InputInfo.MessageRecipient == null)
                {
                    return null;
                }

                if (_directMessageToCache == null ||
                    _directMessageToCache.User.Id != InputInfo.MessageRecipient.Id)
                {
                    _directMessageToCache = new UserViewModel(InputInfo.MessageRecipient);
                }
                return _directMessageToCache;
            }
            set
            {
                if (value == null)
                {
                    InputInfo.MessageRecipient = null;
                    _directMessageToCache = null;
                }
                else
                {
                    InputInfo.MessageRecipient = value.User;
                    _directMessageToCache = value;
                }
                RaisePropertyChanged(() => DirectMessageTo);
                RaisePropertyChanged(() => IsDirectMessageEnabled);
            }
        }

        public bool IsDirectMessageEnabled
        {
            get { return InputInfo.MessageRecipient != null; }
        }

        public ImageDescriptionViewModel AttachedImage
        {
            get { return new ImageDescriptionViewModel(InputInfo.AttachedImage); }
            set
            {
                InputInfo.AttachedImage = value == null ? null : value.Source;

                RaisePropertyChanged(() => AttachedImage);
                RaisePropertyChanged(() => IsImageAttached);
                RaisePropertyChanged(() => CanSaveToDraft);
                UpdateTextCount();
            }
        }

        public bool IsImageAttached
        {
            get { return InputInfo.AttachedImage != null; }
        }

        public bool IsLocationEnabled
        {
            get { return _isLocationEnabled; }
            set
            {
                _isLocationEnabled = value;
                RaisePropertyChanged(() => IsLocationEnabled);
            }
        }

        public LocationDescriptionViewModel AttachedLocation
        {
            get
            {
                if (InputInfo.AttachedGeoInfo != null)
                    return new LocationDescriptionViewModel(InputInfo.AttachedGeoInfo);
                return null;
            }
            set
            {
                InputInfo.AttachedGeoInfo = value == null ? null : value.Location;
                RaisePropertyChanged(() => AttachedLocation);
                RaisePropertyChanged(() => IsLocationAttached);
            }
        }

        public bool IsLocationAttached
        {
            get { return InputInfo.AttachedGeoInfo != null; }
        }

        public int TextCount
        {
            get
            {
                int currentTextLength = StatusTextUtil.CountText(InputText);
                if (IsImageAttached)
                {
                    currentTextLength += Setting.GetImageUploader().UrlLengthPerImages + 1;
                }
                string[] tags = RegexHelper.HashRegex.Matches(InputText)
                                           .OfType<Match>()
                                           .Select(_ => _.Value)
                                           .ToArray();
                if (InputAreaModel.BindingHashtags.Count > 0)
                {
                    currentTextLength += InputAreaModel.BindingHashtags
                                                       .Except(tags)
                                                       .Select(_ => _.Length + 1)
                                                       .Sum();
                }
                return currentTextLength;
            }
        }

        public int RemainTextCount
        {
            get { return StatusTextUtil.MaxTextLength - TextCount; }
        }

        public bool CanSend
        {
            get
            {
                if (AccountSelectionFlip.SelectedAccounts.FirstOrDefault() == null)
                    return false; // send account is not found.
                if (TextCount > StatusTextUtil.MaxTextLength)
                    return false;
                return CanSaveToDraft;
            }
        }

        public bool IsDraftsExisted
        {
            get { return _draftedInputs.Count > 0; }
        }

        public int DraftCount
        {
            get { return _draftedInputs.Count; }
        }

        public bool CanSaveToDraft
        {
            get
            {
                return IsImageAttached || !String.IsNullOrEmpty(
                    InputText.Replace("\t", "")
                             .Replace("\r", "")
                             .Replace("\n", "")
                             .Replace(" ", ""));
            }
        }

        #region Text box control

        private string _selectedText = "";
        private int _selectionLength;
        private int _selectionStart;

        public int SelectionStart
        {
            get { return _selectionStart; }
            set
            {
                _selectionStart = value;
                RaisePropertyChanged(() => SelectionStart);
            }
        }

        public int SelectionLength
        {
            get { return _selectionLength; }
            set
            {
                _selectionLength = value;
                RaisePropertyChanged(() => SelectionLength);
            }
        }

        public string SelectedText
        {
            get { return _selectedText; }
            set
            {
                _selectedText = value;
                RaisePropertyChanged(() => SelectedText);
            }
        }

        #endregion

        #region Post limit prediction properties

        public bool IsPostLimitPredictionEnabled
        {
            get { return InputAreaModel.BindingAuthInfos.Count == 1; }
        }

        public int WindowTime
        {
            get { return 30; }
        }

        public int RemainUpdate
        {
            get { return 32; }
        }

        public int MaxUpdate
        {
            get { return 128; }
        }

        public int MaxControlWidth
        {
            get { return 80; }
        }

        public double ControlWidth
        {
            get { return (double)MaxControlWidth * RemainUpdate / MaxUpdate; }
        }

        /// <summary>
        ///     Start ALPS.
        /// </summary>
        private void InitPostLimitPrediction()
        {
            Observable.Interval(TimeSpan.FromSeconds(60))
                      .Where(_ => IsPostLimitPredictionEnabled)
                      .Subscribe(_ => { });
        }

        #endregion

        private void UpdateHashtagCandidates()
        {
            string[] hashtags = RegexHelper.HashRegex.Matches(InputText)
                                           .OfType<Match>()
                                           .Select(_ => _.Value)
                                           .Distinct()
                                           .Except(BindingHashtags.Select(_ => _.Hashtag))
                                           .ToArray();
            BindableHashtagCandidates
                .Where(_ => !hashtags.Contains(_.Hashtag))
                .ToList()
                .ForEach(_ => BindableHashtagCandidates.Remove(_));
            hashtags
                .Where(_ => !BindableHashtagCandidates.Select(t => t.Hashtag).Contains(_))
                .Select(_ => new BindHashtagViewModel(_, () => BindHashtag(_)))
                .ForEach(BindableHashtagCandidates.Add);
        }

        private void UpdateTextCount()
        {
            RaisePropertyChanged(() => TextCount);
            RaisePropertyChanged(() => RemainTextCount);
            RaisePropertyChanged(() => CanSend);
            RaisePropertyChanged(() => CanSaveToDraft);
        }

        public void OverrideSelectedAccounts(IEnumerable<AuthenticateInfo> infos)
        {
            // if null, not override default.
            if (infos == null) return;
            _isSuppressAccountChangeRelay = true;
            AuthenticateInfo[] accounts = infos as AuthenticateInfo[] ?? infos.ToArray();
            AccountSelectionFlip.SelectedAccounts = accounts;
            InputAreaModel.BindingAuthInfos.Clear();
            accounts.ForEach(InputAreaModel.BindingAuthInfos.Add);
            _isSuppressAccountChangeRelay = false;
        }

        public void ApplyBaseSelectedAccounts()
        {
            _isSuppressAccountChangeRelay = true;
            _accountSelectionFlip.SetSelectedAccountIds(_baseSelectedAccounts);
            _isSuppressAccountChangeRelay = false;
        }

        public void ClearInReplyTo()
        {
            InReplyTo = null;
        }

        public void ClearDirectMessage()
        {
            DirectMessageTo = null;
        }

        public void OpenInput()
        {
            OpenInput(Setting.RestorePreviousStashed.Value);
        }

        public void OpenInput(bool restorePreviousStashed)
        {
            if (!IsOpening)
            {
                IsOpening = true;
                FocusToTextBox();
                if (restorePreviousStashed && InputAreaModel.Drafts.Count > 0)
                {
                    TweetInputInfo last = InputAreaModel.Drafts[InputAreaModel.Drafts.Count - 1];
                    InputAreaModel.Drafts.RemoveAt(InputAreaModel.Drafts.Count - 1);
                    InputInfo = last;
                    Messenger.Raise(new TextBoxSetCaretMessage(InputInfo.Text.Length));
                }
            }
            else
            {
                FocusToTextBox();
            }
        }

        public void CloseInput()
        {
            if (!IsOpening) return;
            if (CheckClearInput())
                IsOpening = false;
        }

        public void FocusToTextBox()
        {
            Messenger.Raise(new InteractionMessage("FocusToTextBox"));
        }

        public bool CheckClearInput(string clearTo = "")
        {
            if (CanSaveToDraft && InputInfo.InitialText != InputInfo.Text)
            {
                TweetBoxClosingAction action = Setting.TweetBoxClosingAction.Value;
                if (action == TweetBoxClosingAction.Confirm)
                {
                    TaskDialogMessage msg = Messenger.GetResponse(new TaskDialogMessage(
                                                                      new TaskDialogOptions
                                                                      {
                                                                          MainInstruction = "現在の内容を下書きに保存しますか？",
                                                                          AllowDialogCancellation = true,
                                                                          CommonButtons =
                                                                              TaskDialogCommonButtons.YesNoCancel,
                                                                          MainIcon = VistaTaskDialogIcon.Information,
                                                                          Title = "下書きへの保存",
                                                                          VerificationText = "次回から表示しない"
                                                                      }));
                    switch (msg.Response.Result)
                    {
                        case TaskDialogSimpleResult.Yes:
                            action = TweetBoxClosingAction.SaveToDraft;
                            break;
                        case TaskDialogSimpleResult.No:
                            action = TweetBoxClosingAction.Discard;
                            break;
                        default:
                            return false;
                    }
                    if (msg.Response.VerificationChecked.GetValueOrDefault())
                    {
                        Setting.TweetBoxClosingAction.Value = action;
                    }
                }
                switch (action)
                {
                    case TweetBoxClosingAction.Discard:
                        break;
                    case TweetBoxClosingAction.SaveToDraft:
                        ClearInput(clearTo, true);
                        return true;
                    default:
                        throw new InvalidOperationException("Invalid return value:" + action.ToString());
                }
            }
            ClearInput(clearTo);
            return true;
        }

        public void ClearInput(string clearTo = "", bool stash = false)
        {
            if (stash && CanSaveToDraft)
            {
                InputAreaModel.Drafts.Add(InputInfo);
            }
            _inputInfo = new TweetInputInfo(clearTo);
            ApplyBaseSelectedAccounts();
            InputInfo.AuthInfos = AccountSelectionFlip.SelectedAccounts;
            RaisePropertyChanged(() => InputInfo);
            RaisePropertyChanged(() => InputText);
            RaisePropertyChanged(() => InReplyTo);
            RaisePropertyChanged(() => IsInReplyToEnabled);
            RaisePropertyChanged(() => DirectMessageTo);
            RaisePropertyChanged(() => IsDirectMessageEnabled);
            RaisePropertyChanged(() => AttachedImage);
            RaisePropertyChanged(() => IsImageAttached);
            RaisePropertyChanged(() => AttachedLocation);
            RaisePropertyChanged(() => IsLocationAttached);
            UpdateHashtagCandidates();
            UpdateTextCount();
        }

        public void AmendPreviousOne()
        {
            if (InputInfo.PostedTweets != null) return; // amending now.
            InputInfo = InputAreaModel.PreviousPosted;
        }

        public void AttachImage()
        {
            var msg = new OpeningFileSelectionMessage
            {
                Filter = "画像ファイル|*.jpg;*.jpeg;*.jpe;*.png;*.gif;*.bmp;*.dib|全てのファイル|*.*",
                InitialDirectory = Setting.LastImageOpenDir.Value,
                MultiSelect = false,
                Title = "添付する画像ファイルを指定"
            };
            OpeningFileSelectionMessage m = Messenger.GetResponse(msg);
            if (m.Response == null || m.Response.Length <= 0 || String.IsNullOrEmpty(m.Response[0]) ||
                !File.Exists(m.Response[0])) return;
            AttachedImage = new ImageDescriptionViewModel(m.Response[0]);
            Setting.LastImageOpenDir.Value = Path.GetDirectoryName(m.Response[0]);
        }

        public void DetachImage()
        {
            AttachedImage = null;
        }

        public void OpenAttachedImage()
        {
        }

        public void AttachLocation()
        {
            AttachedLocation = new LocationDescriptionViewModel(
                _geoWatcher.Position.Location);
        }

        public void DetachLocation()
        {
            AttachedLocation = null;
        }

        public void BindHashtag(string hashtag)
        {
            if (!InputAreaModel.BindingHashtags.Contains(hashtag))
            {
                InputAreaModel.BindingHashtags.Add(hashtag);
                UpdateHashtagCandidates();
                UpdateTextCount();
            }
        }

        public void UnbindHashtag(string hashtag)
        {
            InputAreaModel.BindingHashtags.Remove(hashtag);
            UpdateHashtagCandidates();
            UpdateTextCount();
        }

        private void EscapeUrl()
        {
            string escaped = StatusTextUtil.AutoEscape(InputText);
            if (escaped != InputText)
            {
                InputInfo.Text = escaped;
                RaisePropertyChanged(() => InputText);
                UpdateHashtagCandidates();
                UpdateTextCount();

                int diff = escaped.Length - InputText.Length;
                SelectionStart += diff;
            }
        }

        public void Send()
        {
            if (!CanSend)
            {
                // can't send now.
                RaisePropertyChanged(() => CanSend);
                return;
            }
            if (InputInfo.PostedTweets != null && Setting.IsWarnAmendTweet.Value)
            {
                // amend mode
                TaskDialogMessage amend = Messenger.GetResponse(new TaskDialogMessage(
                                                                    new TaskDialogOptions
                                                                    {
                                                                        Title = "ツイートの訂正",
                                                                        MainIcon = VistaTaskDialogIcon.Information,
                                                                        Content =
                                                                            "直前に投稿されたツイートを削除し、投稿し直します。" +
                                                                            Environment.NewLine +
                                                                            "(削除に失敗した場合でも投稿は行われます。)",
                                                                        ExpandedInfo = "削除されるツイート: " +
                                                                                       InputInfo.PostedTweets.First()
                                                                                                .Item2 +
                                                                                       (InputInfo.PostedTweets.Count() >
                                                                                        1
                                                                                            ? Environment.NewLine + "(" +
                                                                                              (InputInfo.PostedTweets
                                                                                                        .Count() - 1) +
                                                                                              " 件のツイートも同時に削除されます)"
                                                                                            : ""),
                                                                        VerificationText = "次回から表示しない",
                                                                        CommonButtons = TaskDialogCommonButtons.OKCancel,
                                                                    }));
                Setting.IsWarnAmendTweet.Value = amend.Response.VerificationChecked.GetValueOrDefault();
                if (amend.Response.Result == TaskDialogSimpleResult.Cancel)
                    return;
            }
            if (!CheckInput())
                return;
            Send(InputInfo);
            ClearInput();
        }

        private bool CheckInput()
        {
            if (InReplyTo != null && Setting.IsWarnReplyFromThirdAccount.Value)
            {
                // warn third reply

                // filters screen names which were replied
                string[] replies = RegexHelper.AtRegex.Matches(InReplyTo.Status.Text)
                                              .Cast<Match>()
                                              .Select(_ => _.Value.Substring(1))
                                              .Where(_ => !String.IsNullOrEmpty(_))
                                              .Distinct()
                                              .ToArray();

                // check third-reply mistake.
                if (!AccountsStore.Accounts
                                  .Select(_ => _.AuthenticateInfo.UnreliableScreenName)
                                  .Any(replies.Contains) &&
                    InputInfo.AuthInfos
                             .Select(_ => _.UnreliableScreenName)
                             .Any(replies.Contains))
                {
                    TaskDialogMessage thirdreply = Messenger.GetResponse(
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = "割込みリプライ警告",
                            MainIcon = VistaTaskDialogIcon.Warning,
                            Content = "違うアカウントから会話を継続しようとしています。" + Environment.NewLine +
                                      "投稿してもよろしいですか？",
                            VerificationText = "次回から表示しない",
                            CommonButtons = TaskDialogCommonButtons.OKCancel,
                        }));
                    Setting.IsWarnReplyFromThirdAccount.Value =
                        thirdreply.Response.VerificationChecked.GetValueOrDefault();
                    if (thirdreply.Response.Result == TaskDialogSimpleResult.Cancel)
                        return false;
                }
            }
            return true;
        }

        internal static async void Send(TweetInputInfo inputInfo)
        {
            await inputInfo.DeletePrevious();
            inputInfo.Send()
                     .Subscribe(_ =>
                     {
                         Debug.WriteLine("Completed!");
                         if (_.PostedTweets != null)
                         {
                             InputAreaModel.PreviousPosted = _;
                             BackpanelModel.RegisterEvent(new PostSucceededEvent(_));
                         }
                         else
                         {
                             Tuple<bool, string> result = AnalysisFailedReason(_);
                             if (result.Item1)
                                 InputAreaModel.Drafts.Add(_);
                             BackpanelModel.RegisterEvent(new PostFailedEvent(_, result.Item2));
                         }
                     }, ex =>
                     {
                         Debug.WriteLine("Exception is thrown...");
                         Debug.WriteLine(ex);
                     });
        }

        private static Tuple<bool, string> AnalysisFailedReason(TweetInputInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            if (info.ThrownException == null)
                throw new ArgumentException("info.ThrownException is null.");
            string msg = info.ThrownExceptionMessage;
            if (msg != null)
            {
                if (msg.Contains("duplicate"))
                {
                    return Tuple.Create(false, "直近のツイートと重複しています。");
                }
                if (msg.Contains("User is over daily update limit."))
                {
                    return Tuple.Create(true, "POST規制されています。");
                }
                // TODO: Implement more cases.
                return Tuple.Create(true, msg);
            }
            return Tuple.Create(true, info.ThrownException.Message);
        }

        public void SelectAccounts()
        {
            AccountSelectionFlip.Open();
        }
    }

    public class ImageDescriptionViewModel : ViewModel
    {
        public ImageDescriptionViewModel(string p)
        {
            Source = new BitmapImage(new Uri(p));
        }

        public ImageDescriptionViewModel(BitmapImage image)
        {
            Source = image;
        }

        public BitmapImage Source { get; set; }
    }

    public class LocationDescriptionViewModel : ViewModel
    {
        public LocationDescriptionViewModel(GeoCoordinate geoCoordinate)
        {
            Location = new GeoLocationInfo
            {
                Latitude = geoCoordinate.Latitude,
                Longitude = geoCoordinate.Longitude,
            };
        }

        public LocationDescriptionViewModel(GeoLocationInfo locInfo)
        {
            Location = locInfo;
        }

        public GeoLocationInfo Location { get; set; }
    }

    public class TweetInputInfoViewModel : ViewModel
    {
        private readonly Action<TweetInputInfo> _removeHandler;

        public TweetInputInfoViewModel(InputAreaViewModel parent,
                                       TweetInputInfo info, Action<TweetInputInfo> removeHandler)
        {
            Parent = parent;
            Model = info;
            _removeHandler = removeHandler;
        }

        public InputAreaViewModel Parent { get; private set; }

        public TweetInputInfo Model { get; private set; }

        public IEnumerable<AuthenticateInfo> AuthenticateInfos
        {
            get { return Model.AuthInfos; }
        }

        public string Text
        {
            get { return Model.Text; }
        }

        public bool IsFailedTweetInputInfo
        {
            get { return Model.ThrownException != null; }
        }

        public Exception ThrownException
        {
            get { return Model.ThrownException; }
        }

        #region WritebackCommand

        private ViewModelCommand _writebackCommand;

        public ViewModelCommand WritebackCommand
        {
            get { return _writebackCommand ?? (_writebackCommand = new ViewModelCommand(Writeback)); }
        }

        #endregion

        public void Writeback()
        {
            _removeHandler(Model);
            Parent.InputInfo = Model;
        }

        public void Remove()
        {
            _removeHandler(Model);
        }

        public void Send()
        {
            _removeHandler(Model);
            InputAreaViewModel.Send(Model);
        }
    }

    public class AuthenticateInfoViewModel : ViewModel
    {
        private readonly AuthenticateInfo _authInfo;

        public AuthenticateInfoViewModel(AuthenticateInfo info)
        {
            _authInfo = info;
        }

        public AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
        }

        public Uri ProfileImageUri
        {
            get
            {
                if (_authInfo.UnreliableProfileImageUri == null)
                {
                    Task.Run(() => _authInfo.ShowUser(_authInfo.Id)
                                            .Subscribe(_ =>
                                            {
                                                _authInfo.UnreliableProfileImageUriString =
                                                    _.ProfileImageUri.OriginalString;
                                                RaisePropertyChanged(() => ProfileImageUri);
                                            }));
                }
                if (_authInfo.UnreliableProfileImageUri != null)
                {
                    Debug.WriteLine(_authInfo.UnreliableProfileImageUri.OriginalString);
                    return _authInfo.UnreliableProfileImageUri;
                }
                return null;
            }
        }

        public string ScreenName
        {
            get { return _authInfo.UnreliableScreenName; }
        }

        public long Id
        {
            get { return _authInfo.Id; }
        }
    }

    public class BindHashtagViewModel : ViewModel
    {
        private readonly Action _callback;
        private readonly string _hashtag;

        public BindHashtagViewModel(string hashtag, Action callback)
        {
            _hashtag = hashtag;
            _callback = callback;
        }

        public string Hashtag
        {
            get { return _hashtag; }
        }

        public void ToggleBind()
        {
            _callback();
        }
    }
}