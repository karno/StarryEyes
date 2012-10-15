using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Livet;
using StarryEyes.Models;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;
using Livet.EventListeners;
using StarryEyes.ViewModels.WindowParts.Timelines;
using StarryEyes.Settings;
using Livet.Messaging;
using StarryEyes.Views.Messaging;
using System.Windows.Media.Imaging;
using StarryEyes.Models.Operations;

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

        private readonly ReadOnlyDispatcherCollection<string> _bindingHashtags;
        public ReadOnlyDispatcherCollection<string> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        private string _inputText = String.Empty;
        public string InputText
        {
            get { return _inputText; }
            set
            {
                _inputText = value;
                RaisePropertyChanged(() => InputText);
                UpdateTextCount();
            }
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

        private StatusViewModel _inReplyTo = null;
        public StatusViewModel InReplyTo
        {
            get { return _inReplyTo; }
            set
            {
                _inReplyTo = value;
                RaisePropertyChanged(() => InReplyTo);
                RaisePropertyChanged(() => IsInReplyToEnabled);
            }
        }

        public bool IsInReplyToEnabled
        {
            get { return _inReplyTo != null; }
        }

        private UserViewModel _directMessageTo = null;
        public UserViewModel DirectMessageTo
        {
            get { return _directMessageTo; }
            set
            {
                _directMessageTo = value;
                RaisePropertyChanged(() => DirectMessageTo);
                RaisePropertyChanged(() => IsDirectMessageEnabled);
            }
        }

        public bool IsDirectMessageEnabled
        {
            get { return _directMessageTo != null; }
        }
        
        private ImageDescriptionViewModel _attachedImage = null;
        public ImageDescriptionViewModel AttachedImage
        {
            get { return _attachedImage; }
            set
            {
                _attachedImage = value;
                RaisePropertyChanged(() => AttachedImage);
                RaisePropertyChanged(() => IsImageAttached);
                UpdateTextCount();
            }
        }

        public bool IsImageAttached
        {
            get { return _attachedImage != null; }
        }

        private LocationDescriptionViewModel _attachedLocation = null;
        public LocationDescriptionViewModel AttachedLocation
        {
            get { return _attachedLocation; }
            set
            {
                _attachedLocation = value;
                RaisePropertyChanged(() => AttachedLocation);
                RaisePropertyChanged(() => IsLocationAttached);
            }
        }

        public bool IsLocationAttached
        {
            get { return _attachedLocation != null; }
        }

        private void UpdateTextCount()
        {
            RaisePropertyChanged(() => TextCount);
            RaisePropertyChanged(() => CanSend);
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

        public InputAreaViewModel()
        {
            this._accountSelector = new AccountSelectorViewModel();
            this._bindingHashtags = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                InputAreaModel.BindingHashtags, _ => _, DispatcherHelper.UIDispatcher);
            this._accountSelector.OnSelectedAccountsChanged += () =>
            {
                if (!_isSuppressAccountChangeRelay)
                {
                    // write-back
                    InputAreaModel.BindingAuthInfos.Replace(this._accountSelector.SelectedAccounts);
                    _baseSelectedAccounts = InputAreaModel.BindingAuthInfos.Select(_ => _.Id).ToArray();
                }
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
                    ClearInput();
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
                        ClearInput();
                        OverrideSelectedAccounts(infos);
                        DirectMessageTo = new UserViewModel(user);
                    }));
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
            InReplyTo = null;
        }

        public void ClearInput()
        {
            InReplyTo = null;
            DirectMessageTo = null;
            AttachedImage = null;
            AttachedLocation = null;
            InputText = String.Empty;
            ApplyBaseSelectedAccounts();
        }

        public void Send()
        {
            var currentAccounts = this.AccountSelector.SelectedAccounts.ToArray();
            if (IsDirectMessageEnabled)
            {
                InputAreaModel.SendMessage(
                    this.AccountSelector.SelectedAccounts.ToArray(),
                    this.InputText,
                    DirectMessageTo.User);
            }
            else
            {
                InputAreaModel.SendStatus(
                    this.AccountSelector.SelectedAccounts.ToArray(),
                    this.InputText,
                    InReplyTo != null ? InReplyTo.Status : null,
                    AttachedImage != null ? AttachedImage.Source : null,
                    AttachedLocation != null ? AttachedLocation.Location : null);
            }
            ClearInput();
        }
    }

    public class ImageDescriptionViewModel : ViewModel
    {
        public BitmapImage Source { get; set; }
    }

    public class LocationDescriptionViewModel : ViewModel
    {
        public GeoLocationInfo Location { get; set; }
    }
}