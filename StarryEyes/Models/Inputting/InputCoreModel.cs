using System;
using System.Collections.Generic;
using System.Linq;
using Cadena.Data;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.Models.Inputting
{
    public class InputCoreModel : NotificationObject
    {
        private readonly ObservableSynchronizedCollectionEx<string> _bindingHashtags =
            new ObservableSynchronizedCollectionEx<string>();

        private readonly ObservableSynchronizedCollectionEx<InputData> _drafts =
            new ObservableSynchronizedCollectionEx<InputData>();

        private InputData _inputData = new InputData(String.Empty);

        private InputData _lastPostedData;

        private TabModel _currentFocusTabModel;

        #region properties

        [NotNull]
        public ObservableSynchronizedCollectionEx<string> BindingHashtags => _bindingHashtags;

        [NotNull]
        public ObservableSynchronizedCollectionEx<InputData> Drafts => _drafts;

        [NotNull]
        internal InputData CurrentInputData
        {
            get => _inputData;
            set
            {
                if (value == _inputData) return;
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (_inputData != null && _inputData.IsChanged)
                {
                    _drafts.Add(_inputData);
                }
                _inputData = value;
                _inputData.BoundTags = _inputData.IsDirectMessage
                    ? Enumerable.Empty<string>()
                    : BindingHashtags.ToArray();
                RaisePropertyChanged(() => CurrentInputData);
            }
        }

        [CanBeNull]
        public InputData LastPostedData
        {
            get => _lastPostedData;
            internal set
            {
                _lastPostedData = value;
                RaisePropertyChanged(() => LastPostedData);
                RaisePropertyChanged(() => CanAmend);
            }
        }

        public bool IsAmending => CurrentInputData.IsAmend;

        public bool CanAmend => LastPostedData != null && CurrentInputData != LastPostedData;

        #endregion properties

        #region events

        internal event Action<CursorPosition> SetCursorRequest;

        internal event Action FocusRequest;

        internal event Action CloseRequest;

        #endregion events

        internal InputCoreModel()
        {
            CurrentInputData = new InputData(String.Empty);
            _bindingHashtags.ListenCollectionChanged(_ =>
            {
                if (_currentFocusTabModel != null)
                {
                    _currentFocusTabModel.BindingHashtags = _bindingHashtags.ToArray();
                }
            });
        }

        internal void ChangeFocusingTab(TabModel previous, TabModel replace)
        {
            _currentFocusTabModel = null;
            if (previous != null)
            {
                previous.BindingHashtags = _bindingHashtags.ToArray();
            }
            _bindingHashtags.Clear();
            replace?.BindingHashtags
                   .ToArray()
                   .ForEach(_bindingHashtags.Add);
            _currentFocusTabModel = replace;
        }

        public void SetText([CanBeNull] InputSetting setting)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            if (setting.Recipient != null)
            {
                SetDirectMessage(setting.Accounts, setting.Recipient, setting.SetFocusToInputArea);
            }
            else
            {
                SetText(setting.Accounts,
                    setting.Body,
                    setting.InReplyTo,
                    setting.CursorPosition,
                    setting.SetFocusToInputArea);
            }
        }

        private void SetText(IEnumerable<TwitterAccount> infos,
            string body,
            TwitterStatus inReplyTo,
            CursorPosition cursor,
            bool focusToInputArea)
        {
            CurrentInputData = new InputData(body)
            {
                Accounts = infos,
                InReplyTo = inReplyTo
            };
            SetCursorRequest?.Invoke(cursor ?? CursorPosition.End);
            if (focusToInputArea)
            {
                FocusRequest?.Invoke();
            }
        }

        private void SetDirectMessage(IEnumerable<TwitterAccount> infos,
            [CanBeNull] TwitterUser recipient,
            bool focusToInputArea)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            CurrentInputData = new InputData(String.Empty)
            {
                Accounts = infos,
                MessageRecipient = recipient
            };
            // because text is always empty, setting cursor position can be skipped.
            if (focusToInputArea)
            {
                FocusRequest?.Invoke();
            }
        }

        public void AmendLastPosted()
        {
            if (LastPostedData != null)
            {
                CurrentInputData = _lastPostedData;
            }
        }

        public void ClearInput(string text, bool sendDraftIfChanged)
        {
            var current = _inputData;
            _inputData = new InputData(text);
            if (current.IsChanged && sendDraftIfChanged)
            {
                // if text is not changed, send to draft
                _drafts.Add(current);
            }
            _inputData.BoundTags = BindingHashtags.ToArray();
            RaisePropertyChanged(() => CurrentInputData);
        }

        public void Close()
        {
            CloseRequest?.Invoke();
        }
    }
}