using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public class InputCoreModel : NotificationObject
    {
        private readonly ObservableSynchronizedCollection<string> _bindingHashtags =
            new ObservableSynchronizedCollection<string>();

        private readonly ObservableSynchronizedCollection<InputData> _drafts =
            new ObservableSynchronizedCollection<InputData>();

        private InputData _inputData;

        private InputData _amendingTarget;

        #region properties

        [NotNull]
        internal InputData CurrentInputData
        {
            get { return _inputData; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _inputData = value;
                RaisePropertyChanged(() => CurrentInputData);
            }
        }

        [CanBeNull]
        public InputData AmendingTarget
        {
            get { return _amendingTarget; }
            set
            {
                _amendingTarget = value;
                RaisePropertyChanged(() => AmendingTarget);
            }
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

        public void SetText(string body = null,
                            CursorPosition cursor = null,
                            TwitterStatus inReplyTo = null,
                            IEnumerable<TwitterAccount> infos = null,
                            bool focusToInputArea = true)
        {

            CurrentInputData = new InputData(body)
            {
                Accounts = infos,
                InReplyTo = inReplyTo,
            };
            var ch = SetCursorRequest;
            if (ch != null)
            {
                ch(cursor ?? CursorPosition.End);
            }
            var fh = FocusRequest;
            if (focusToInputArea && fh != null)
            {
                fh();
            }
        }

        public void SetText(string body = null,
                            CursorPosition cursor = null,
                            TwitterStatus inReplyTo = null,
                            IEnumerable<long> infos = null,
                            bool focusToInputArea = true)
        {
            var accounts = infos == null
                               ? null
                               : infos.Select(Setting.Accounts.Get).Where(s => s != null);
            SetText(body,
                    cursor,
                    inReplyTo,
                    accounts,
                    focusToInputArea);
        }

        public void SetDirectMessage([NotNull] TwitterUser recipient,
                                     IEnumerable<TwitterAccount> infos = null,
                                     bool focusToInputArea = true)
        {
            if (recipient == null) throw new ArgumentNullException("recipient");
            CurrentInputData = new InputData(String.Empty)
            {
                Accounts = infos,
                MessageRecipient = recipient
            };
            // because text is always empty, setting cursor position can be skipped.
            var fh = FocusRequest;
            if (focusToInputArea && fh != null)
            {
                fh();
            }
        }

        public void ClearInput(bool sendDraftIfChanged)
        {
            var current = _inputData;
            _inputData = new InputData(String.Empty);
            if (current.IsChanged && sendDraftIfChanged)
            {
                // if text is not changed, send to draft
                _drafts.Add(current);
            }
        }

        public void Close()
        {
            var handler = CloseRequest;
            if (handler != null)
            {
                handler();
            }
        }

    }
}
