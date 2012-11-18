using Livet;
using Livet.EventListeners;
using Livet.Messaging.IO;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.PostEvents;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Views.Messaging;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using TaskDialogInterop;

namespace StarryEyes.ViewModels.WindowParts
{
    public class InputAreaViewModel : ViewModel
    {
        private bool _isSuppressAccountChangeRelay = false;
        private long[] _baseSelectedAccounts;

        private readonly AccountSelectorViewModel _accountSelector;
        public AccountSelectorViewModel AccountSelector
        {
            get { return _accountSelector; }
        }

        private readonly ReadOnlyDispatcherCollection<BindHashtagViewModel> _bindingHashtags;
        public ReadOnlyDispatcherCollection<BindHashtagViewModel> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        private readonly DispatcherCollection<BindHashtagViewModel> _bindableHashtagCandidates;
        public DispatcherCollection<BindHashtagViewModel> BindableHashtagCandidates
        {
            get { return _bindableHashtagCandidates; }
        }

        private readonly ReadOnlyDispatcherCollection<TweetInputInfoViewModel> _draftedInputs;
        public ReadOnlyDispatcherCollection<TweetInputInfoViewModel> DraftedInputs
        {
            get { return _draftedInputs; }
        }

        public IEnumerable<BindHashtagViewModel> DesignSource { get { return new[] { "Entropy", "Negentropy" }.Select(_ => new BindHashtagViewModel(_, () => { })); } }

        private readonly ReadOnlyDispatcherCollection<AuthenticateInfoViewModel> _bindingAuthInfos;
        public ReadOnlyDispatcherCollection<AuthenticateInfoViewModel> BindingAuthInfos
        {
            get { return _bindingAuthInfos; }
        }

        public int AuthInfoGridRowColumn
        {
            get
            {
                return (int)Math.Ceiling(Math.Sqrt(Math.Max(_bindingAuthInfos.Count, 1)));
            }
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

        private bool _isOpening = false;
        public bool IsOpening
        {
            get { return _isOpening; }
            set
            {
                if (_isOpening == value) return;
                _isOpening = value;
                RaisePropertyChanged(() => IsOpening);
                if (value)
                    this.Messenger.RaiseAsync(new GoToStateMessage("Open"));
                else
                    this.Messenger.RaiseAsync(new GoToStateMessage("Close"));
            }
        }

        private TweetInputInfo _inputInfo = null;
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
                UpdateTextCount();
            }
        }

        public string InputText
        {
            get { return InputInfo.Text; }
            set
            {
                InputInfo.Text = value;
                RaisePropertyChanged(() => InputText);
                UpdateTextCount();
                UpdateHashtagCandidates();
            }
        }

        private void UpdateHashtagCandidates()
        {
            var hashtags = RegexHelper.HashRegex.Matches(this.InputText)
                .OfType<Match>()
                .Select(_ => _.Value)
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

        public bool IsUrlAutoEsacpeEnabled
        {
            get { return Setting.IsUrlAutoEscapeEnabled.Value; }
            set
            {
                Setting.IsUrlAutoEscapeEnabled.Value = value;
                RaisePropertyChanged(() => IsUrlAutoEsacpeEnabled);
            }
        }

        private StatusViewModel _inReplyToViewModelCache = null;
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

        private UserViewModel _directMessageToCache = null;
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
                InputInfo.AttachedImage = value.Source;
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

        private bool _isLocationEnabled = false;
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
                else
                    return null;
            }
            set
            {
                if (value == null)
                    InputInfo.AttachedGeoInfo = null;
                else
                    InputInfo.AttachedGeoInfo = value.Location;
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
                if (this.IsImageAttached)
                {
                    currentTextLength += Setting.GetImageUploader().UrlLengthPerImages + 1;
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
                if (this.AccountSelector.SelectedAccounts.FirstOrDefault() == null)
                    return false; // send account is not found.
                if (TextCount > StatusTextUtil.MaxTextLength)
                    return false;
                return true;
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
            get { return IsImageAttached || !String.IsNullOrEmpty(InputText); }
        }

        #region Post limit prediction properties

        public bool IsPostLimitPredictionEnabled
        {
            get { return false; }
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

        /// <summary>
        /// update state of ALPS.
        /// </summary>
        private void UpdatePostLimitPredictionState()
        {
        }

        public int MaxControlWidth
        {
            get { return 80; }
        }

        public int ControlWidth
        {
            get { return (int)((double)MaxControlWidth * RemainUpdate / MaxUpdate); }
        }

        #endregion

        private GeoCoordinateWatcher geoWatcher;

        /// <summary>
        /// Constructor
        /// </summary>
        public InputAreaViewModel()
        {
            this._accountSelector = new AccountSelectorViewModel();

            this.CompositeDisposable.Add(this._bindingHashtags =
                ViewModelHelper.CreateReadOnlyDispatcherCollection(
                InputAreaModel.BindingHashtags,
                _ => new BindHashtagViewModel(_, () => UnbindHashtag(_)),
                DispatcherHelper.UIDispatcher));

            this._bindableHashtagCandidates =
                new DispatcherCollection<BindHashtagViewModel>(DispatcherHelper.UIDispatcher);

            this.CompositeDisposable.Add(this._draftedInputs =
                ViewModelHelper.CreateReadOnlyDispatcherCollection(
                InputAreaModel.Drafts,
                _ => new TweetInputInfoViewModel(this, _, vm => InputAreaModel.Drafts.Remove(vm)),
                DispatcherHelper.UIDispatcher));

            this.CompositeDisposable.Add(new CollectionChangedEventListener(this._draftedInputs,
                (o, ev) =>
                {
                    RaisePropertyChanged(() => DraftCount);
                    RaisePropertyChanged(() => IsDraftsExisted);
                }));

            this.CompositeDisposable.Add(this._bindingAuthInfos =
                ViewModelHelper.CreateReadOnlyDispatcherCollection(
                InputAreaModel.BindingAuthInfos,
                _ => new AuthenticateInfoViewModel(_),
                DispatcherHelper.UIDispatcher));

            this._accountSelector.OnSelectedAccountsChanged += () =>
            {
                if (!_isSuppressAccountChangeRelay)
                {
                    // write-back
                    InputAreaModel.BindingAuthInfos.Replace(this._accountSelector.SelectedAccounts);
                    _baseSelectedAccounts = InputAreaModel.BindingAuthInfos.Select(_ => _.Id).ToArray();
                }
                InputInfo.AuthInfos = this.AccountSelector.SelectedAccounts;
                RaisePropertyChanged(() => AuthInfoGridRowColumn);
                UpdateTextCount();
            };
            this.CompositeDisposable.Add(_accountSelector);
            this.CompositeDisposable.Add(
                new CollectionChangedEventListener(
                    InputAreaModel.BindingAuthInfos,
                    (_, __) =>
                    {
                        _baseSelectedAccounts = InputAreaModel.BindingAuthInfos
                            .Select(a => a.Id)
                            .ToArray();
                        ApplyBaseSelectedAccounts();
                        UpdateTextCount();
                    }));
            this.CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus>>(
                _ => InputAreaModel.OnSetTextRequested += _,
                _ => InputAreaModel.OnSetTextRequested -= _,
                (infos, body, cursor, inReplyTo) =>
                {
                    OpenInput(false);
                    CheckClearInput();
                    OverrideSelectedAccounts(infos);
                    InputText = body;
                    InReplyTo = new StatusViewModel(inReplyTo);
                    switch (cursor)
                    {
                        case CursorPosition.Begin:
                            this.Messenger.RaiseAsync(new TextBoxSetCaretMessage(0));
                            break;
                        case CursorPosition.End:
                            this.Messenger.RaiseAsync(new TextBoxSetCaretMessage(InputText.Length));
                            break;
                    }
                }));
            this.CompositeDisposable.Add(
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

            geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            geoWatcher.StatusChanged += (_, e) =>
            {
                if (e.Status != GeoPositionStatus.Ready)
                {
                    this.IsLocationEnabled = true;
                }
                else
                {
                    this.IsLocationEnabled = false;
                    this.AttachedLocation = null;
                }
            };
            this.CompositeDisposable.Add(geoWatcher);
            geoWatcher.Start();
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
            _isSuppressAccountChangeRelay = true;
            AccountSelector.SelectedAccounts = infos;
            _isSuppressAccountChangeRelay = false;
        }

        public void ApplyBaseSelectedAccounts()
        {
            _isSuppressAccountChangeRelay = true;
            _accountSelector.SetSelectedAccountIds(_baseSelectedAccounts);
            _isSuppressAccountChangeRelay = false;
        }

        public void ClearInReplyTo()
        {
            this.InReplyTo = null;
        }

        public void ClearDirectMessage()
        {
            this.DirectMessageTo = null;
        }

        public void OpenInput()
        {
            this.OpenInput(true);
        }

        public void OpenInput(bool restorePreviousStashed)
        {
            this.IsOpening = true;
            if (restorePreviousStashed && InputAreaModel.Drafts.Count > 0)
            {
                var last = InputAreaModel.Drafts[InputAreaModel.Drafts.Count - 1];
                InputAreaModel.Drafts.RemoveAt(InputAreaModel.Drafts.Count - 1);
                this.InputInfo = last;
            }
        }

        public void CloseInput()
        {
            if (!this.IsOpening) return;
            CheckClearInput();
        }

        private void CheckClearInput()
        {
            if (!String.IsNullOrWhiteSpace(InputText) ||
                this.IsImageAttached)
            {
                var action = Setting.TweetBoxClosingAction.Value;
                if (action == TweetBoxClosingAction.Confirm)
                {
                    var msg = this.Messenger.GetResponse(new TaskDialogMessage(
                        new TaskDialogOptions()
                        {
                            AllowDialogCancellation = true,
                            CommonButtons = TaskDialogCommonButtons.YesNoCancel,
                            Content = "現在の内容を下書きに保存しますか？",
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
                            return;
                    }
                    if (msg.Response.VerificationChecked.GetValueOrDefault())
                    {
                        Setting.TweetBoxClosingAction.Value = action;
                    }
                }
                switch (action)
                {
                    case TweetBoxClosingAction.Discard:
                        ClearInput();
                        break;
                    case TweetBoxClosingAction.SaveToDraft:
                        StashInDraft();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid return value:" + action.ToString());
                }
            }
            else
            {
                ClearInput();
            }
        }

        public void ClearInput()
        {
            this._inputInfo = new TweetInputInfo();
            ApplyBaseSelectedAccounts();
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
            UpdateTextCount();
        }

        public void AmendPreviousOne()
        {
            if (InputInfo.PostedTweets != null) return; // amending now.
            if (!String.IsNullOrEmpty(InputText) || this.IsImageAttached)
                StashInDraft();
            InputInfo = InputAreaModel.PreviousPosted;
        }

        public void StashInDraft()
        {
            InputAreaModel.Drafts.Add(this.InputInfo);
            ClearInput();
        }

        public void AttachImage()
        {
            var msg = new OpeningFileSelectionMessage();
            msg.Filter = "画像ファイル|*.jpg;*.jpeg;*.jpe;*.png;*.gif;*.bmp;*.dib|全てのファイル|*.*";
            msg.InitialDirectory = Setting.LastImageOpenDir.Value;
            msg.MultiSelect = false;
            msg.Title = "添付する画像ファイルを指定";
            this.Messenger.GetResponseAsync(msg, m =>
            {
                if (m.Response.Length > 0)
                {
                    this.AttachedImage = new ImageDescriptionViewModel(m.Response[0]);
                }
            });
        }

        public void DetachImage()
        {
            this.AttachedImage = null;
        }

        public void OpenAttachedImage()
        {
        }

        public void AttachLocation()
        {
            this.AttachedLocation = new LocationDescriptionViewModel(
                geoWatcher.Position.Location);
        }

        public void DetachLocation()
        {
            this.AttachedLocation = null;
        }

        public void BindHashtag(string hashtag)
        {
            if (!InputAreaModel.BindingHashtags.Contains(hashtag))
            {
                InputAreaModel.BindingHashtags.Add(hashtag);
                UpdateHashtagCandidates();
            }
        }

        public void UnbindHashtag(string hashtag)
        {
            InputAreaModel.BindingHashtags.Remove(hashtag);
            UpdateHashtagCandidates();
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
                var amend = this.Messenger.GetResponse(new TaskDialogMessage(
                    new TaskDialogInterop.TaskDialogOptions()
                    {
                        Title = "ツイートの訂正",
                        MainIcon = VistaTaskDialogIcon.Information,
                        Content = "直前に投稿されたツイートを削除し、投稿し直します。" + Environment.NewLine +
                            "(削除に失敗した場合でも投稿は行われます。)",
                        ExpandedInfo = "削除されるツイート: " +
                        InputInfo.PostedTweets.First().Item2.ToString() +
                        (InputInfo.PostedTweets.Count() > 1 ?
                            Environment.NewLine + "(" + (InputInfo.PostedTweets.Count() - 1) +
                            " 件のツイートも同時に削除されます)" : ""),
                        VerificationText = "次回から表示しない",
                        CommonButtons = TaskDialogCommonButtons.OKCancel,
                    }));
                Setting.IsWarnAmendTweet.Value = amend.Response.VerificationChecked.GetValueOrDefault();
                if (amend.Response.Result == TaskDialogSimpleResult.Cancel)
                    return;
            }
            if (!CheckInput())
                return;
            this.Send(InputInfo);
            ClearInput();
        }

        private bool CheckInput()
        {
            if (InReplyTo != null && Setting.IsWarnReplyFromThirdAccount.Value)
            {
                // warn third reply

                // filters screen names which were replied
                var replies = RegexHelper.AtRegex.Matches(InReplyTo.Status.Text)
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
                    var thirdreply = this.Messenger.GetResponse(
                        new TaskDialogMessage(new TaskDialogOptions()
                        {
                            Title = "割込みリプライ警告",
                            MainIcon = VistaTaskDialogIcon.Warning,
                            Content = "違うアカウントから会話を継続しようとしています。" + Environment.NewLine +
                            "投稿してもよろしいですか？",
                            VerificationText = "次回から表示しない",
                            CommonButtons = TaskDialogCommonButtons.OKCancel,
                        }));
                    Setting.IsWarnReplyFromThirdAccount.Value = thirdreply.Response.VerificationChecked.GetValueOrDefault();
                    if (thirdreply.Response.Result == TaskDialogSimpleResult.Cancel)
                        return false;
                }
            }
            return true;
        }

        internal void Send(TweetInputInfo inputInfo)
        {
            inputInfo.Send()
                .Subscribe(_ =>
                {
                    if (_.PostedTweets != null)
                    {
                        InputAreaModel.PreviousPosted = _;
                        BackpanelModel.RegisterEvent(new PostSucceededEvent(_));
                    }
                    else
                    {
                        var result = AnalysisFailedReason(_);
                        if (result.Item1)
                            InputAreaModel.Drafts.Add(_);
                        BackpanelModel.RegisterEvent(new PostFailedEvent(_, result.Item2));
                    }
                });
        }

        private Tuple<bool, string> AnalysisFailedReason(TweetInputInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            if (info.ThrownException == null)
                throw new ArgumentException("info.ThrownException is null.");
            var msg = info.ThrownExceptionMessage;
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
            else
            {
                return Tuple.Create(true, info.ThrownException.Message);
            }
        }
    }

    public class ImageDescriptionViewModel : ViewModel
    {
        public ImageDescriptionViewModel(string p)
        {
            this.Source = new BitmapImage(new Uri(p));
        }

        public ImageDescriptionViewModel(BitmapImage image)
        {
            this.Source = image;
        }

        public BitmapImage Source { get; set; }
    }

    public class LocationDescriptionViewModel : ViewModel
    {
        public LocationDescriptionViewModel(GeoCoordinate geoCoordinate)
        {
            this.Location = new GeoLocationInfo()
            {
                Latitude = geoCoordinate.Latitude,
                Longitude = geoCoordinate.Longitude,
            };
        }

        public LocationDescriptionViewModel(GeoLocationInfo locInfo)
        {
            this.Location = locInfo;
        }

        public GeoLocationInfo Location { get; set; }
    }

    public class TweetInputInfoViewModel : ViewModel
    {
        public InputAreaViewModel Parent { get; private set; }

        public TweetInputInfo Model { get; private set; }

        private Action<TweetInputInfo> _removeHandler;

        public TweetInputInfoViewModel(InputAreaViewModel parent,
            TweetInputInfo info, Action<TweetInputInfo> removeHandler)
        {
            this.Parent = parent;
            this.Model = info;
            this._removeHandler = removeHandler;
        }

        public IEnumerable<AuthenticateInfo> AuthenticateInfos { get { return Model.AuthInfos; } }

        public string Text { get { return Model.Text; } }

        public bool IsFailedTweetInputInfo
        {
            get { return Model.ThrownException != null; }
        }

        public Exception ThrownException { get { return Model.ThrownException; } }

        public void Writeback()
        {
            _removeHandler(this.Model);
            Parent.InputInfo = this.Model;
        }

        public void Remove()
        {
            _removeHandler(this.Model);
        }

        public void Send()
        {
            _removeHandler(this.Model);
            Parent.Send(this.Model);
        }
    }

    public class AuthenticateInfoViewModel : ViewModel
    {
        private readonly AuthenticateInfo _authInfo;
        public AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
        }

        public AuthenticateInfoViewModel(AuthenticateInfo info)
        {
            this._authInfo = info;
        }

        public Uri ProfileImageUri
        {
            get { return _authInfo.UnreliableProfileImageUri; }
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
        private readonly string _hashtag;
        public string Hashtag
        {
            get { return _hashtag; }
        }

        private readonly Action _callback;

        public BindHashtagViewModel(string hashtag, Action callback)
        {
            this._hashtag = hashtag;
            this._callback = callback;
        }

        public void ToggleBind()
        {
            _callback();
        }
    }
}