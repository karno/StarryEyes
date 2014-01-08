using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Helpers;
using StarryEyes.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents.PostEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Requests;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Common;
using StarryEyes.ViewModels.Timelines.Statuses;
using StarryEyes.ViewModels.WindowParts.Flips;
using StarryEyes.Views.Controls;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts
{
    public class InputAreaViewModel : ViewModel
    {
        private readonly AccountSelectionFlipViewModel _accountSelectionFlip;
        private readonly DispatcherCollection<BindHashtagViewModel> _bindableHashtagCandidates;
        private readonly ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> _bindingAuthInfos;

        private readonly ReadOnlyDispatcherCollectionRx<BindHashtagViewModel> _bindingHashtags;
        private readonly ReadOnlyDispatcherCollectionRx<TweetInputInfoViewModel> _draftedInputs;
        private readonly InputAreaSuggestItemProvider _provider;

        private readonly GeoCoordinateWatcher _geoWatcher;
        private long[] _baseSelectedAccounts;
        private UserViewModel _directMessageToCache;
        private StatusViewModel _inReplyToViewModelCache;
        private TweetInputInfo _inputInfo;
        private bool _isLocationEnabled;
        private bool _isOpening;
        private bool _suppressAccountChangeRelay;

        private ClipboardWatcher _watcher;

        /// <summary>
        ///     Constructor
        /// </summary>
        public InputAreaViewModel()
        {
            _provider = new InputAreaSuggestItemProvider();

            #region Account control
            _accountSelectionFlip = new AccountSelectionFlipViewModel();
            _accountSelectionFlip.Closed += () =>
            {
                // After selection accounts, return focus to text box
                // if input area is opened.
                if (IsOpening)
                {
                    FocusToTextBox();
                }
            };
            var accountSelectReflecting = false;
            _accountSelectionFlip.SelectedAccountsChanged += () =>
            {
                if (!this._suppressAccountChangeRelay)
                {
                    // write-back
                    accountSelectReflecting = true;
                    InputAreaModel.BindingAccounts.Clear();
                    _accountSelectionFlip.SelectedAccounts
                                    .ForEach(InputAreaModel.BindingAccounts.Add);
                    accountSelectReflecting = false;
                    _baseSelectedAccounts = InputAreaModel.BindingAccounts.Select(_ => _.Id).ToArray();
                }
                InputInfo.Accounts = AccountSelectionFlip.SelectedAccounts;
                RaisePropertyChanged(() => AuthInfoGridRowColumn);
                UpdateTextCount();
                RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
            };
            CompositeDisposable.Add(_accountSelectionFlip);
            CompositeDisposable.Add(
                new CollectionChangedEventListener(
                    InputAreaModel.BindingAccounts,
                    (_, __) =>
                    {
                        RaisePropertyChanged(() => IsPostLimitPredictionEnabled);
                        if (accountSelectReflecting) return;
                        _baseSelectedAccounts = InputAreaModel.BindingAccounts
                                                              .Select(a => a.Id)
                                                              .ToArray();
                        ApplyBaseSelectedAccounts();
                        UpdateTextCount();
                    }));
            #endregion

            CompositeDisposable.Add(_bindingHashtags =
                                    ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                                        InputAreaModel.BindingHashtags,
                                        _ => new BindHashtagViewModel(_, () => UnbindHashtag(_)),
                                        DispatcherHelper.UIDispatcher));
            CompositeDisposable.Add(_bindingHashtags
                                        .ListenCollectionChanged()
                                        .Subscribe(_ =>
                                        {
                                            InputInfo.Hashtags = _bindingHashtags.Select(h => h.Hashtag).ToArray();
                                            RaisePropertyChanged(() => IsBindingHashtagExisted);
                                        }));

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
                                        InputAreaModel.BindingAccounts,
                                        account => new TwitterAccountViewModel(account),
                                        DispatcherHelper.UIDispatcher));

            CompositeDisposable.Add(_bindingAuthInfos
                                    .ListenCollectionChanged()
                                    .Subscribe(_ => this.RaisePropertyChanged(() => AuthInfoGridRowColumn)));

            CompositeDisposable.Add(_bindingAuthInfos
                                        .ListenCollectionChanged()
                                        .Subscribe(_ => RaisePropertyChanged(() => IsBindingAuthInfoExisted)));

            CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<TwitterAccount>, string, CursorPosition, TwitterStatus>>(
                    h => InputAreaModel.SetTextRequested += h,
                    h => InputAreaModel.SetTextRequested -= h, (infos, body, cursor, inReplyTo) =>
                    {
                        OpenInput(false);
                        if (!CheckClearInput(body)) return;
                        if (infos != null)
                        {
                            OverrideSelectedAccounts(infos);
                        }
                        if (inReplyTo != null)
                        {
                            Task.Run(async () => InReplyTo = new StatusViewModel(await StatusModel.Get(inReplyTo)));
                        }
                        var offset = cursor.Index;
                        if (offset < 0)
                        {
                            offset = InputText.Length - offset + 1;
                        }
                        Messenger.Raise(new TextBoxSetCaretMessage(offset, cursor.SelectionLength));
                    }));

            CompositeDisposable.Add(
                new EventListener<Action<IEnumerable<TwitterAccount>, TwitterUser, string>>(
                    _ => InputAreaModel.SendDirectMessageRequested += _,
                    _ => InputAreaModel.SendDirectMessageRequested -= _,
                    (infos, user, body) =>
                    {
                        OpenInput(false);
                        if (!CheckClearInput(body)) return;
                        OverrideSelectedAccounts(infos);
                        DirectMessageTo = new UserViewModel(user);
                    }));
            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputAreaModel.OnPreviousPostUpdated += h,
                    h => InputAreaModel.OnPreviousPostUpdated -= h,
                    () => this.RaisePropertyChanged(() => CanAmend)));

            CompositeDisposable.Add(InitPostLimitPrediction());

            CompositeDisposable.Add(_watcher = new ClipboardWatcher());
            _watcher.ClipboardChanged += (o, e) => RaisePropertyChanged(() => this.IsClipboardContentImage);
            _watcher.StartWatching();

            if (!Setting.DisableGeoLocationService.Value)
            {
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

            this.RegisterEvents();
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

        public ReadOnlyDispatcherCollectionRx<TwitterAccountViewModel> BindingAuthInfos
        {
            get { return _bindingAuthInfos; }
        }

        public InputAreaSuggestItemProvider Provider
        {
            get { return _provider; }
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

        [NotNull]
        public TweetInputInfo InputInfo
        {
            get
            {
                if (_inputInfo == null)
                {
                    ClearInput();
                }
                Debug.Assert(_inputInfo != null, "_inputInfo != null");
                return _inputInfo;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (CanSaveToDraft)
                {
                    InputAreaModel.Drafts.Add(InputInfo);
                }
                _inputInfo = value;
                value.Hashtags = this._bindingHashtags.Select(b => b.Hashtag).ToArray();
                OverrideSelectedAccounts(value.Accounts);
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
                RaisePropertyChanged(() => IsAmending);
                RaisePropertyChanged(() => CanAmend);
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

                if (_inReplyToViewModelCache != null &&
                    _inReplyToViewModelCache.Status.Id != InputInfo.InReplyTo.Status.Id)
                {
                    _inReplyToViewModelCache.Dispose();
                    _inReplyToViewModelCache = null;
                }
                return this._inReplyToViewModelCache ??
                       (this._inReplyToViewModelCache = new StatusViewModel(this.InputInfo.InReplyTo));
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
                    InputInfo.InReplyTo = value.Model;
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

        public bool IsAmending
        {
            get { return this.InputInfo.PostedTweets != null; }
        }

        public int TextCount
        {
            get
            {
                var currentTextLength = StatusTextUtil.CountText(InputText);
                if (IsImageAttached)
                {
                    currentTextLength += TwitterConfigurationService.HttpsUrlLength;
                }
                var tags = TwitterRegexPatterns.ValidHashtag.Matches(InputText)
                                           .OfType<Match>()
                                           .Select(_ => _.Groups[1].Value)
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
            get { return TwitterConfigurationService.TextMaxLength - TextCount; }
        }

        public bool CanSend
        {
            get
            {
                if (AccountSelectionFlip.SelectedAccounts.FirstOrDefault() == null)
                    return false; // send account is not found.
                if (TextCount > TwitterConfigurationService.TextMaxLength)
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

        public bool CanAmend
        {
            get { return InputAreaModel.PreviousPosted != null && !IsAmending; }
        }

        #region Key assign control

        private static bool _isRegisterEvents;
        private void RegisterEvents()
        {
            if (_isRegisterEvents) throw new InvalidOperationException();
            _isRegisterEvents = true;
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("CloseInput", this.CloseInput),
                KeyAssignAction.Create("Post", this.Send),
                KeyAssignAction.Create("LoadStash", () =>
                {
                    if (this.IsDraftsExisted)
                    {
                        _draftedInputs[0].Writeback();
                    }
                }),
                KeyAssignAction.Create("Amend", this.AmendPreviousOne),
                KeyAssignAction.Create("AttachImage", () =>
                {
                    if (this.IsImageAttached)
                    {
                        this.DetachImage();
                    }
                    else
                    {
                        this.AttachImage();
                    }
                }),
                KeyAssignAction.Create("ToggleEscape",
                                       () => this.IsUrlAutoEsacpeEnabled = !this.IsUrlAutoEsacpeEnabled)
                );
        }

        #endregion

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
            get { return InputAreaModel.BindingAccounts.Count == 1; }
        }

        public int RemainUpdate { get; set; }

        public int MaxUpdate { get; set; }

        public bool IsWarningPostLimit
        {
            get { return RemainUpdate < 5; }
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
        private IDisposable InitPostLimitPrediction()
        {
            return Observable.Interval(TimeSpan.FromSeconds(5))
                      .Where(_ => IsPostLimitPredictionEnabled)
                      .Subscribe(_ =>
                      {
                          var account = InputAreaModel.BindingAccounts.FirstOrDefault();
                          if (account == null) return;
                          var count = PostLimitPredictionService.GetCurrentWindowCount(account.Id);
                          MaxUpdate = Setting.PostLimitPerWindow.Value;
                          RemainUpdate = MaxUpdate - count;
                          this.RaisePropertyChanged(() => RemainUpdate);
                          this.RaisePropertyChanged(() => MaxUpdate);
                          this.RaisePropertyChanged(() => ControlWidth);
                          this.RaisePropertyChanged(() => IsWarningPostLimit);
                      });
        }

        #endregion

        private void UpdateHashtagCandidates()
        {
            var hashtags = TwitterRegexPatterns.ValidHashtag.Matches(InputText)
                                               .OfType<Match>()
                                               .Select(_ => _.Groups[1].Value)
                                               .Where(s => !String.IsNullOrEmpty(s))
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

        public void OverrideSelectedAccounts(IEnumerable<TwitterAccount> infos)
        {
            // if null, not override default.
            if (infos == null) return;
            this._suppressAccountChangeRelay = true;
            var accounts = infos as TwitterAccount[] ?? infos.ToArray();
            AccountSelectionFlip.SelectedAccounts = accounts;
            InputAreaModel.BindingAccounts.Clear();
            accounts.ForEach(InputAreaModel.BindingAccounts.Add);
            this._suppressAccountChangeRelay = false;
        }

        public void ApplyBaseSelectedAccounts()
        {
            this._suppressAccountChangeRelay = true;
            _accountSelectionFlip.SetSelectedAccountIds(_baseSelectedAccounts);
            this._suppressAccountChangeRelay = false;
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
                    var last = InputAreaModel.Drafts[InputAreaModel.Drafts.Count - 1];
                    InputAreaModel.Drafts.RemoveAt(InputAreaModel.Drafts.Count - 1);
                    InputInfo = last;
                    Messenger.Raise(new TextBoxSetCaretMessage(InputInfo.Text.Length, 0));
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
            // move focus to timeline
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
        }

        public void FocusToTextBox()
        {
            Messenger.Raise(new InteractionMessage("FocusToTextBox"));
        }

        public bool CheckClearInput(string clearTo = "")
        {
            if (CanSaveToDraft && InputInfo.InitialText != InputInfo.Text)
            {
                var action = Setting.TweetBoxClosingAction.Value;
                if (action == TweetBoxClosingAction.Confirm)
                {
                    var msg = Messenger.GetResponse(
                        new TaskDialogMessage(
                            new TaskDialogOptions
                            {
                                Title = "下書きへの保存",
                                MainIcon = VistaTaskDialogIcon.Information,
                                MainInstruction = "現在の内容を下書きに保存しますか？",
                                CustomButtons = new[] { "保存(&Y)", "破棄(&N)", "キャンセル" },
                                VerificationText = "次回から表示しない",
                                AllowDialogCancellation = true,
                            }));
                    switch (msg.Response.CustomButtonResult)
                    {
                        case 0:
                            action = TweetBoxClosingAction.SaveToDraft;
                            break;
                        case 1:
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
            _inputInfo = new TweetInputInfo(clearTo)
            {
                Hashtags = _bindingHashtags.Select(t => t.Hashtag).ToArray()
            };
            ApplyBaseSelectedAccounts();
            InputInfo.Accounts = AccountSelectionFlip.SelectedAccounts;
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
            RaisePropertyChanged(() => IsAmending);
            RaisePropertyChanged(() => CanAmend);
            UpdateHashtagCandidates();
            UpdateTextCount();
        }

        public void AmendPreviousOne()
        {
            // if already amending or previous post is not existed, return.
            if (InputInfo.PostedTweets != null || InputAreaModel.PreviousPosted == null) return;
            InputInfo = InputAreaModel.PreviousPosted;
        }

        #region Image attach control

        public void AttachImage()
        {
            var msg = new OpeningFileSelectionMessage
            {
                Filter = "画像ファイル|*.jpg;*.jpeg;*.jpe;*.png;*.gif;*.bmp;*.dib|全てのファイル|*.*",
                InitialDirectory = Setting.LastImageOpenDir.Value,
                MultiSelect = false,
                Title = "添付する画像ファイルを指定"
            };
            var m = Messenger.GetResponse(msg);
            if (m.Response == null || m.Response.Length <= 0 ||
                String.IsNullOrEmpty(m.Response[0]) || !File.Exists(m.Response[0]))
            {
                return;
            }

            Setting.LastImageOpenDir.Value = Path.GetDirectoryName(m.Response[0]);
            AttachImageFromPath(m.Response[0]);
        }

        public void DetachImage()
        {
            AttachedImage = null;
        }

        public void AttachClipboardImage()
        {
            BitmapSource image;
            if (!Clipboard.ContainsImage() || (image = Clipboard.GetImage()) == null) return;
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
            using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fs);
            }
            AttachImageFromPath(tempPath);
        }

        private bool AttachImageFromPath(string file)
        {
            try
            {
                AttachedImage = new ImageDescriptionViewModel(file);
                return true;
            }
            catch (Exception ex)
            {
                this.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "画像読み込みエラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "画像の添付ができませんでした。",
                    Content = "画像の読み込み時にエラーが発生しました。" + Environment.NewLine +
                              "未対応の画像か、データが破損しています。",
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                }));
                AttachedImage = null;
                return false;
            }
        }

        public void StartSnippingTool()
        {
            try
            {
                Process.Start("SnippingTool.exe");
            }
            catch (Exception ex)
            {
                this.Messenger.Raise(new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = "エラー",
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction = "Snipping Toolの起動に失敗しました。",
                    Content = "スタートメニューからの起動を試してみてください。",
                    ExpandedInfo = ex.ToString(),
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
            }
        }

        public bool IsClipboardContentImage
        {
            get { return Clipboard.ContainsImage(); }
        }

        private DropAcceptDescription _description;

        public DropAcceptDescription DropAcceptDescription
        {
            get
            {
                if (_description == null)
                {
                    _description = new DropAcceptDescription();
                    _description.DragOver += args =>
                    {
                        args.Effects = args.Data.GetData(DataFormats.FileDrop) != null
                                           ? DragDropEffects.Link
                                           : DragDropEffects.None;
                        args.Handled = true;
                    };
                    _description.DragDrop += args =>
                    {
                        var files = args.Data.GetData(DataFormats.FileDrop) as string[];
                        if (files != null && files.Length > 0)
                        {
                            AttachedImage = new ImageDescriptionViewModel(files[0]);
                        }
                    };
                }
                return _description;
            }
        }


        #endregion

        public void AttachLocation()
        {
            if (_geoWatcher == null || _geoWatcher.Status != GeoPositionStatus.Ready) return;
            AttachedLocation = new LocationDescriptionViewModel(
                _geoWatcher.Position.Location);
        }

        public void DetachLocation()
        {
            AttachedLocation = null;
        }

        public void BindHashtag(string hashtag)
        {
            if (InputAreaModel.BindingHashtags.Contains(hashtag)) return;
            InputAreaModel.BindingHashtags.Add(hashtag);
            this.UpdateHashtagCandidates();
            this.UpdateTextCount();
        }

        public void UnbindHashtag(string hashtag)
        {
            InputAreaModel.BindingHashtags.Remove(hashtag);
            UpdateHashtagCandidates();
            UpdateTextCount();
        }

        private void EscapeUrl()
        {
            var escaped = StatusTextUtil.AutoEscape(InputText);
            if (escaped == this.InputText) return;
            this.InputInfo.Text = escaped;
            this.RaisePropertyChanged(() => this.InputText);
            this.UpdateHashtagCandidates();
            this.UpdateTextCount();

            var diff = escaped.Length - this.InputText.Length;
            this.SelectionStart += diff;
        }

        public void Send()
        {
            if (!CanSend)
            {
                // can't send now.
                RaisePropertyChanged(() => CanSend);
                FocusToTextBox();
                return;
            }
            if (InputInfo.PostedTweets != null && Setting.WarnAmending.Value)
            {
                var removal = InputInfo.PostedTweets.First().Item2.ToString();
                var dual = InputInfo.PostedTweets.Count();
                if (dual > 2)
                {
                    removal += " (" + dual + "件のツイートが同時に削除されます)";
                }
                // amend mode
                var amend = Messenger.GetResponse(
                    new TaskDialogMessage(
                        new TaskDialogOptions
                        {
                            Title = "ツイートの訂正",
                            MainIcon = VistaTaskDialogIcon.Information,
                            MainInstruction = "直前のツイートを削除し、再投稿します。",
                            Content = "削除に失敗した場合でも投稿は行われます。",
                            ExpandedInfo = "削除されるツイート: " + removal,
                            CommonButtons = TaskDialogCommonButtons.OKCancel,
                            VerificationText = "次回から表示しない",
                        }));
                Setting.WarnAmending.Value = !amend.Response.VerificationChecked.GetValueOrDefault();
                if (amend.Response.Result == TaskDialogSimpleResult.Cancel)
                {
                    return;
                }
            }
            if (!CheckInput())
                return;
            Send(InputInfo);
            ClearInput();
            FocusToTextBox();
        }

        private bool CheckInput()
        {
            if (InReplyTo != null && Setting.WarnReplyFromThirdAccount.Value)
            {
                // warn third reply

                // filters screen names which were replied
                var replies = TwitterRegexPatterns.ValidMentionOrList.Matches(InReplyTo.Status.Text)
                                                  .Cast<Match>()
                                                  .Select(_ => _.Groups[TwitterRegexPatterns.ValidMentionOrListGroupUsername].Value.Substring(1))
                                                  .Where(_ => !String.IsNullOrEmpty(_))
                                                  .Distinct()
                                                  .ToArray();

                // check third-reply mistake.
                if (!Setting.Accounts
                            .Collection
                            .Select(a => a.UnreliableScreenName)
                            .Any(replies.Contains) &&
                    InputInfo.Accounts
                             .Select(_ => _.UnreliableScreenName)
                             .Any(replies.Contains))
                {
                    var thirdreply = Messenger.GetResponse(
                        new TaskDialogMessage(new TaskDialogOptions
                        {
                            Title = "割込みリプライ警告",
                            MainIcon = VistaTaskDialogIcon.Warning,
                            Content = "違うアカウントから会話を継続しようとしています。" + Environment.NewLine +
                                      "投稿してもよろしいですか？",
                            VerificationText = "次回から表示しない",
                            CommonButtons = TaskDialogCommonButtons.OKCancel,
                        }));
                    Setting.WarnReplyFromThirdAccount.Value =
                        !thirdreply.Response.VerificationChecked.GetValueOrDefault();
                    if (thirdreply.Response.Result == TaskDialogSimpleResult.Cancel)
                        return false;
                }
            }
            return true;
        }

        internal static void Send(TweetInputInfo inputInfo)
        {
            Task.Run(async () =>
            {
                await inputInfo.DeletePreviousAsync();
                inputInfo.Send()
                         .Subscribe(tweetInputInfo =>
                         {
                             if (tweetInputInfo.PostedTweets != null)
                             {
                                 InputAreaModel.PreviousPosted = tweetInputInfo;
                                 BackstageModel.RegisterEvent(new PostSucceededEvent(tweetInputInfo));
                             }
                             else
                             {
                                 var result = AnalysisFailedReason(tweetInputInfo);
                                 if (result.Item1)
                                 {
                                     InputAreaModel.Drafts.Add(tweetInputInfo);
                                 }
                                 BackstageModel.RegisterEvent(new PostFailedEvent(tweetInputInfo, result.Item2));
                             }
                         }, ex => Debug.WriteLine(ex));
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
            AccountSelectionFlip.Open();
        }
    }

    public class ImageDescriptionViewModel : ViewModel
    {
        public ImageDescriptionViewModel(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(filePath);
            bitmap.EndInit();
            Source = bitmap;
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

        public IEnumerable<TwitterAccount> TwitterAccounts
        {
            get { return Model.Accounts; }
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

    public class BindHashtagViewModel : ViewModel
    {
        private readonly Action _callback;
        private readonly string _hashtag;

        public BindHashtagViewModel(string hashtag, Action callback)
        {
            _hashtag = hashtag;
            _callback = callback;
        }

        public string DisplayHashtag
        {
            get { return "#" + _hashtag; }
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

    public class InputAreaSuggestItemProvider : SuggestItemProviderBase
    {
        public override int CandidateSelectionIndex { get; set; }
        public override string FindNearestToken(string text, int caretIndex, out int tokenStart, out int tokenLength)
        {
            tokenStart = caretIndex - 1;
            tokenLength = 1;
            while (tokenStart >= 0)
            {
                if (CheckTriggerCharInputted(text[tokenStart]))
                {
                    return text.Substring(tokenStart, tokenLength);
                }
                tokenStart--;
                tokenLength++;
            }
            return null;
        }

        public override void UpdateCandidateList(string token, int offset)
        {
            if (String.IsNullOrEmpty(token) || (token[0] != '@' && token[0] != '#'))
            {
                _items.Clear();
            }
            else
            {
                if (token[0] == '@')
                {
                    var sn = token.Substring(1);
                    if (token.Length == 1)
                    {
                        // pre-clear
                        _items.Clear();
                    }
                    Task.Run(() => AddUserItems(sn));
                }
                else
                {
                    _items.Clear();
                    AddHashItems(token.Substring(1));
                    this.SelectSuitableItem(token);
                }
            }
        }

        private void SelectSuitableItem(string token)
        {
            var array = _items.Select(s => s.Body.Substring(1))
            .ToArray();
            while (token.Length > 1)
            {
                var find = token.Substring(1);
                var idx = array.Select((v, i) => new { Item = v, Index = i })
                               .Where(t => t.Item.StartsWith(find, StringComparison.CurrentCultureIgnoreCase))
                               .Select(t => t.Index)
                               .Append(-1)
                               .First();
                if (idx >= 0)
                {
                    CandidateSelectionIndex = idx;
                    RaisePropertyChanged("CandidateSelectionIndex");
                    break;
                }
                token = token.Substring(0, token.Length - 1);
            }
        }

        public override bool CheckTriggerCharInputted(char inputchar)
        {
            switch (inputchar)
            {
                case '@':
                case '#':
                    return true;
            }
            return false;
        }

        private async Task AddUserItems(string key)
        {
            Debug.WriteLine("current screen name: " + key);
            var items = (await UserProxy.GetUsersFastAsync(key))
                .Select(t => t.Item2)
                .ToArray();
            // re-ordering
            var ordered = items.Where(s => s.StartsWith(key))
                               .OrderBy(s => s)
                               .Concat(items.Where(s => !s.StartsWith(key))
                                            .OrderBy(s => s))
                               .Select(s => new SuggestItemViewModel("@" + s))
                               .ToArray();
            await DispatcherHelper.UIDispatcher.InvokeAsync(
                () =>
                {
                    _items.Clear();
                    ordered.ForEach(s => _items.Add(s));
                    this.SelectSuitableItem("@" + key);
                });
        }

        private void AddHashItems(string key)
        {
            CacheStore.HashtagCache
                      .Where(s => String.IsNullOrEmpty(key) ||
                          s.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                      .OrderBy(_ => _)
                      .Select(s => new SuggestItemViewModel("#" + s))
                      .ForEach(s => _items.Add(s));
        }

        private readonly ObservableCollection<SuggestItemViewModel> _items = new ObservableCollection<SuggestItemViewModel>();
        public override IList CandidateCollection
        {
            get { return _items; }
        }
    }

    public class SuggestItemViewModel : ViewModel
    {
        public SuggestItemViewModel(string body)
        {
            this.Body = body;
        }

        public string Body { get; set; }

        public override string ToString()
        {
            return Body;
        }
    }
}