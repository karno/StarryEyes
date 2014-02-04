using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Albireo;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;

namespace StarryEyes.Models.Inputting
{
    public class InputCoreModel : NotificationObject
    {
        private readonly ObservableSynchronizedCollection<string> _bindingHashtags =
            new ObservableSynchronizedCollection<string>();

        private readonly ObservableSynchronizedCollection<InputData> _drafts =
            new ObservableSynchronizedCollection<InputData>();

        private InputData _inputData = new InputData(String.Empty);

        private InputData _lastPostedData;

        #region properties

        [NotNull]
        public ObservableSynchronizedCollection<string> BindingHashtags
        {
            get { return _bindingHashtags; }
        }

        [NotNull]
        public ObservableSynchronizedCollection<InputData> Drafts
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
                _inputData.BoundTags = BindingHashtags.ToArray();
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
