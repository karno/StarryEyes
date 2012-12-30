﻿using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Livet;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;
using StarryEyes.Models.Backpanels.PostEvents;
using StarryEyes.Models.Operations;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Views.Messaging;
using TaskDialogInterop;

namespace StarryEyes.ViewModels.WindowParts
{
    public class InputAreaViewModel : ViewModel
    {
        private bool _isSuppressAccountChangeRelay;
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

        public bool IsBindingHashtagExisted
        {
            get { return _bindingHashtags != null && _bindingHashtags.Count > 0; }
        }

        private readonly DispatcherCollection<BindHashtagViewModel> _bindableHashtagCandidates;
        public DispatcherCollection<BindHashtagViewModel> BindableHashtagCandidates
        {
            get { return _bindableHashtagCandidates; }
        }

        public bool IsBindableHashtagExisted
        {
            get { return _bindableHashtagCandidates != null && _bindableHashtagCandidates.Count > 0; }
        }

        private readonly ReadOnlyDispatcherCollection<TweetInputInfoViewModel> _draftedInputs;
        public ReadOnlyDispatcherCollection<TweetInputInfoViewModel> DraftedInputs
        {
            get { return _draftedInputs; }
        }

        public bool IsBindingAuthInfoExisted
        {
            get { return _bindingAuthInfos != null && _bindingAuthInfos.Count > 0; }
        }

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

        private bool _isOpening;
        public bool IsOpening
        {
            get { return _isOpening; }
            set
            {
                if (_isOpening == value) return;
                _isOpening = value;
                RaisePropertyChanged(() => IsOpening);
                this.Messenger.RaiseAsync(value ? new GoToStateMessage("Open") : new GoToStateMessage("Close"));
            }
        }

        private TweetInputInfo _inputInfo;
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

        private void UpdateHashtagCandidates()
        {
            var hashtags = RegexHelper.HashRegex.Matches(this.InputText)
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

        private StatusViewModel _inReplyToViewModelCache;
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

        private UserViewModel _directMessageToCache;
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

        private bool _isLocationEnabled;
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
                if (this.IsImageAttached)
                {
                    currentTextLength += Setting.GetImageUploader().UrlLengthPerImages + 1;
                }
                var tags = RegexHelper.HashRegex.Matches(InputText)
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
                if (this.AccountSelector.SelectedAccounts.FirstOrDefault() == null)
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
            get { return IsImageAttached || !String.IsNullOrEmpty(InputText.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace(" ", "")); }
        }

        #region Text box control

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

        private int _selectionLength;
        public int SelectionLength
        {
            get { return _selectionLength; }
            set
            {
                _selectionLength = value;
                RaisePropertyChanged(() => SelectionLength);
            }
        }

        private string _selectedText = "";
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
        /// Start ALPS.
        /// </summary>
        private void InitPostLimitPrediction()
        {
            Observable.Interval(TimeSpan.FromSeconds(0.5))
                      .Where(_ => IsPostLimitPredictionEnabled)
                      .Subscribe(_ =>
                      {
                          RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
                      });
        }

        #endregion

        private readonly GeoCoordinateWatcher _geoWatcher;

        /// <summary>
        /// Constructor
        /// </summary>
        public InputAreaViewModel()
        {
            this._accountSelector = new AccountSelectorViewModel();
            this._accountSelector.OnClosed += () =>
            {
                // After selection accounts, return focus to text box
                // if input area is opened.
                if (IsOpening)
                {
                    FocusToTextBox();
                }
            };

            this.CompositeDisposable.Add(this._bindingHashtags =
                ViewModelHelper.CreateReadOnlyDispatcherCollection(
                InputAreaModel.BindingHashtags,
                _ => new BindHashtagViewModel(_, () => UnbindHashtag(_)),
                DispatcherHelper.UIDispatcher));
            this.CompositeDisposable.Add(new CollectionChangedEventListener(
                this._bindingHashtags, (_, __) => RaisePropertyChanged(() => IsBindingHashtagExisted)));

            this._bindableHashtagCandidates =
                new DispatcherCollection<BindHashtagViewModel>(DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(new CollectionChangedEventListener(
                this._bindableHashtagCandidates, (_, __) => RaisePropertyChanged(() => IsBindableHashtagExisted)));

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

            this.CompositeDisposable.Add(new CollectionChangedEventListener(
                this._bindingAuthInfos, (_, __) => RaisePropertyChanged(() => IsBindingAuthInfoExisted)));

            bool accountSelectReflecting = false;
            this._accountSelector.OnSelectedAccountsChanged += () =>
            {
                if (!_isSuppressAccountChangeRelay)
                {
                    // write-back
                    accountSelectReflecting = true;
                    InputAreaModel.BindingAuthInfos.Clear();
                    this._accountSelector.SelectedAccounts
                        .ForEach(InputAreaModel.BindingAuthInfos.Add);
                    accountSelectReflecting = false;
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
                        if (accountSelectReflecting) return;
                        _baseSelectedAccounts = InputAreaModel.BindingAuthInfos
                            .Select(a => a.Id)
                            .ToArray();
                        ApplyBaseSelectedAccounts();
                        UpdateTextCount();
                        RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
                    }));
            this.CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus>>(
                _ => InputAreaModel.OnSetTextRequested += _,
                _ => InputAreaModel.OnSetTextRequested -= _,
                (infos, body, cursor, inReplyTo) =>
                {
                    OpenInput(false);
                    CheckClearInput();
                    if (infos != null)
                    {
                        OverrideSelectedAccounts(infos);
                    }
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

            _geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            _geoWatcher.StatusChanged += (_, e) =>
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
            this.CompositeDisposable.Add(_geoWatcher);
            _geoWatcher.Start();
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
            var accounts = infos as AuthenticateInfo[] ?? infos.ToArray();
            AccountSelector.SelectedAccounts = accounts;
            InputAreaModel.BindingAuthInfos.Clear();
            accounts.ForEach(InputAreaModel.BindingAuthInfos.Add);
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
            this.OpenInput(Setting.RestorePreviousStashed.Value);
        }

        public void OpenInput(bool restorePreviousStashed)
        {
            if (!this.IsOpening)
            {
                this.IsOpening = true;
                FocusToTextBox();
                if (restorePreviousStashed && InputAreaModel.Drafts.Count > 0)
                {
                    var last = InputAreaModel.Drafts[InputAreaModel.Drafts.Count - 1];
                    InputAreaModel.Drafts.RemoveAt(InputAreaModel.Drafts.Count - 1);
                    this.InputInfo = last;
                    this.Messenger.Raise(new TextBoxSetCaretMessage(this.InputInfo.Text.Length));
                }
            }
            else
            {
                FocusToTextBox();
            }
        }

        public void CloseInput()
        {
            if (!this.IsOpening) return;
            CheckClearInput();
            this.IsOpening = false;
        }

        public void FocusToTextBox()
        {
            this.Messenger.Raise(new InteractionMessage("FocusToTextBox"));
        }

        private void CheckClearInput()
        {
            if (CanSaveToDraft)
            {
                var action = Setting.TweetBoxClosingAction.Value;
                if (action == TweetBoxClosingAction.Confirm)
                {
                    var msg = this.Messenger.GetResponse(new TaskDialogMessage(
                        new TaskDialogOptions
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
            InputInfo.AuthInfos = this.AccountSelector.SelectedAccounts;
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
            if (CanSaveToDraft)
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
            var msg = new OpeningFileSelectionMessage
            {
                Filter = "画像ファイル|*.jpg;*.jpeg;*.jpe;*.png;*.gif;*.bmp;*.dib|全てのファイル|*.*",
                InitialDirectory = Setting.LastImageOpenDir.Value,
                MultiSelect = false,
                Title = "添付する画像ファイルを指定"
            };
            var m = this.Messenger.GetResponse(msg);
            if (m.Response != null && m.Response.Length > 0)
            {
                this.AttachedImage = new ImageDescriptionViewModel(m.Response[0]);
            }
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
                _geoWatcher.Position.Location);
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
            var escaped = StatusTextUtil.AutoEscape(this.InputText);
            if (escaped != this.InputText)
            {
                InputInfo.Text = escaped;
                RaisePropertyChanged(() => InputText);
                UpdateHashtagCandidates();
                UpdateTextCount();

                var diff = escaped.Length - this.InputText.Length;
                this.SelectionStart += diff;
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
                var amend = this.Messenger.GetResponse(new TaskDialogMessage(
                    new TaskDialogOptions
                    {
                        Title = "ツイートの訂正",
                        MainIcon = VistaTaskDialogIcon.Information,
                        Content = "直前に投稿されたツイートを削除し、投稿し直します。" + Environment.NewLine +
                            "(削除に失敗した場合でも投稿は行われます。)",
                        ExpandedInfo = "削除されるツイート: " +
                        InputInfo.PostedTweets.First().Item2 +
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
            Send(InputInfo);
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
                        new TaskDialogMessage(new TaskDialogOptions
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

        internal async static void Send(TweetInputInfo inputInfo)
        {
            await inputInfo.DeletePrevious();
            inputInfo.Send()
                .Subscribe(_ =>
                {
                    System.Diagnostics.Debug.WriteLine("Completed!");
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
                }, ex =>
                {
                    System.Diagnostics.Debug.WriteLine("Exception is thrown...");
                    System.Diagnostics.Debug.WriteLine(ex);
                });
        }

        private static Tuple<bool, string> AnalysisFailedReason(TweetInputInfo info)
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
            return Tuple.Create(true, info.ThrownException.Message);
        }

        public void SelectAccounts()
        {
            this.AccountSelector.Open();
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
            this.Location = new GeoLocationInfo
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

        private readonly Action<TweetInputInfo> _removeHandler;

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

        #region WritebackCommand
        private Livet.Commands.ViewModelCommand _writebackCommand;

        public Livet.Commands.ViewModelCommand WritebackCommand
        {
            get { return _writebackCommand ?? (_writebackCommand = new Livet.Commands.ViewModelCommand(Writeback)); }
        }
        #endregion

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
            InputAreaViewModel.Send(this.Model);
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
            get
            {
                if (_authInfo.UnreliableProfileImageUri == null)
                {
                    Task.Run(() => _authInfo.ShowUser(_authInfo.Id)
                        .Subscribe(_ =>
                        {
                            _authInfo.UnreliableProfileImageUriString = _.ProfileImageUri.OriginalString;
                            RaisePropertyChanged(() => ProfileImageUri);
                        }));
                }
                if (_authInfo.UnreliableProfileImageUri != null)
                {
                    System.Diagnostics.Debug.WriteLine(_authInfo.UnreliableProfileImageUri.OriginalString);
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