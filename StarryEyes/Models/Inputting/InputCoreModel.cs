using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
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
        public ObservableSynchronizedCollectionEx<string> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        [NotNull]
        public ObservableSynchronizedCollectionEx<InputData> Drafts
        {
            get { return _drafts; }
        }

        [NotNull]
        internal InputData CurrentInputData
        {
            get { return _inputData; }
            set
            {
                if (value == _inputData) return;
                if (value == null) throw new ArgumentNullException("value");
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
            get { return this._lastPostedData; }
            internal set
            {
                this._lastPostedData = value;
                RaisePropertyChanged(() => LastPostedData);
                this.RaisePropertyChanged(() => CanAmend);
            }
        }

        public bool IsAmending
        {
            get { return CurrentInputData.IsAmend; }
        }

        public bool CanAmend
        {
            get { return LastPostedData != null && CurrentInputData != LastPostedData; }
        }

        #endregion

        #region events

        internal event Action<CursorPosition> SetCursorRequest;

        internal event Action FocusRequest;

        internal event Action CloseRequest;

        #endregion

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
            if (replace != null)
            {
                replace.BindingHashtags
                       .ToArray()
                       .ForEach(_bindingHashtags.Add);
            }
            _currentFocusTabModel = replace;
        }

        public void SetText([NotNull] InputSetting setting)
        {
            if (setting == null) throw new ArgumentNullException("setting");
            if (setting.Recipient != null)
            {
                this.SetDirectMessage(setting.Accounts, setting.Recipient, setting.SetFocusToInputArea);
            }
            else
            {
                this.SetText(setting.Accounts,
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
                InReplyTo = inReplyTo,
            };
            SetCursorRequest.SafeInvoke(cursor ?? CursorPosition.End);
            if (focusToInputArea)
            {
                FocusRequest.SafeInvoke();
            }
        }

        private void SetDirectMessage(IEnumerable<TwitterAccount> infos,
                                      [NotNull] TwitterUser recipient,
                                      bool focusToInputArea)
        {
            if (recipient == null) throw new ArgumentNullException("recipient");
            CurrentInputData = new InputData(String.Empty)
            {
                Accounts = infos,
                MessageRecipient = recipient
            };
            // because text is always empty, setting cursor position can be skipped.
            if (focusToInputArea)
            {
                FocusRequest.SafeInvoke();
            }
        }

        public void AmendLastPosted()
        {
            if (LastPostedData != null)
            {
                this.CurrentInputData = _lastPostedData;
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
            CloseRequest.SafeInvoke();
        }

    }
}
