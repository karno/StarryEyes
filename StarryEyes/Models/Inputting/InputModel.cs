using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Cadena.Data;
using JetBrains.Annotations;
using Livet.EventListeners;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public static class InputModel
    {
        private static readonly AccountSelectorModel _accounts;

        private static readonly InputCoreModel _core;

        static InputModel()
        {
            var disposables = new CompositeDisposable();
            _core = new InputCoreModel();
            _accounts = new AccountSelectorModel(_core);

            // link property changing
            var icmpc = new PropertyChangedEventListener(_core);
            icmpc.RegisterHandler(() => _core.CurrentInputData,
                (o, e) => _accounts.CurrentInputDataChanged());
            disposables.Add(icmpc);
            SetEventPropagation();

            App.ApplicationFinalize += () => disposables.Dispose();
        }

        public static void Initialize()
        {
            StatusBroadcaster.BroadcastPoint.Subscribe(s =>
            {
                var status = s.StatusModel?.Status;
                if (status == null) return;
                UpdateForUser(status.User, status.CreatedAt);
                UpdateForUser(status.Recipient, status.CreatedAt);
                UpdateForUser(status.RetweetedStatus?.User, status.RetweetedStatus?.CreatedAt);
                UpdateForUser(status.QuotedStatus?.User, status.QuotedStatus?.CreatedAt);
            }, _ => { }, () => { });
        }

        private static void UpdateForUser([CanBeNull] TwitterUser user, DateTime? timestamp)
        {
            if (user == null || timestamp == null) return;
            AccountProxy.UpdateUserInfoAsync(user, timestamp.Value);
        }

        #region composite events

        private static void SetEventPropagation()
        {
            _core.SetCursorRequest += arg => SetCursorRequest?.Invoke(arg);
            _core.FocusRequest += () => FocusRequest?.Invoke();
            _core.CloseRequest += () => CloseRequest?.Invoke();
        }

        internal static event Action<CursorPosition> SetCursorRequest;

        internal static event Action FocusRequest;

        internal static event Action CloseRequest;

        #endregion composite events

        #region Internal Models

        internal static AccountSelectorModel AccountSelector => _accounts;

        internal static InputCoreModel InputCore => _core;

        #endregion Internal Models

        #region Proxy methods for plugins

        public static void SelectAccount([CanBeNull] IEnumerable<TwitterAccount> accounts)
        {
            if (accounts == null) throw new ArgumentNullException(nameof(accounts));
            _accounts.Accounts.Clear();
            Setting.Accounts.Collection
                   .Where(accounts.Contains)
                   .ForEach(a => _accounts.Accounts.Add(a));
        }

        public static void SetText([CanBeNull] InputSetting setting)
        {
            _core.SetText(setting);
        }

        public static void AmendLastPosted()
        {
            _core.AmendLastPosted();
        }

        public static void Close()
        {
            _core.Close();
        }

        public static void ClearInput()
        {
            _core.ClearInput(String.Empty, true);
        }

        public static void ClearBindingHashtags()
        {
            _core.BindingHashtags.Clear();
        }

        public static void BindHashtag(string hashtag)
        {
            if (_core.BindingHashtags.Contains(hashtag)) return;
            _core.BindingHashtags.Add(hashtag);
        }

        #endregion Proxy methods for plugins
    }
}