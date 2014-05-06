using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using Livet.EventListeners;
using StarryEyes.Albireo;
using StarryEyes.Models.Accounting;
using StarryEyes.Settings;

namespace StarryEyes.Models.Inputting
{
    public static class InputModel
    {
        private static readonly CompositeDisposable _disposables;

        private static readonly AccountSelectorModel _accounts;

        private static readonly InputCoreModel _core;

        static InputModel()
        {
            _disposables = new CompositeDisposable();
            _core = new InputCoreModel();
            _accounts = new AccountSelectorModel(_core);

            // link property changing
            var icmpc = new PropertyChangedEventListener(_core);
            icmpc.RegisterHandler(() => _core.CurrentInputData,
                                (o, e) => _accounts.CurrentInputDataChanged());
            _disposables.Add(icmpc);
            SetEventPropagation();

            App.ApplicationFinalize += () => _disposables.Dispose();
        }

        #region composite events

        private static void SetEventPropagation()
        {
            _core.SetCursorRequest += arg => SetCursorRequest.SafeInvoke(arg);
            _core.FocusRequest += () => FocusRequest.SafeInvoke();
            _core.CloseRequest += () => CloseRequest.SafeInvoke();
        }

        internal static event Action<CursorPosition> SetCursorRequest;

        internal static event Action FocusRequest;

        internal static event Action CloseRequest;

        #endregion

        #region Internal Models

        internal static AccountSelectorModel AccountSelector
        {
            get { return _accounts; }
        }

        internal static InputCoreModel InputCore
        {
            get { return _core; }
        }

        #endregion

        #region Proxy methods for plugins

        public static void SelectAccount([NotNull] IEnumerable<TwitterAccount> accounts)
        {
            if (accounts == null) throw new ArgumentNullException("accounts");
            _accounts.Accounts.Clear();
            Setting.Accounts.Collection
                   .Where(accounts.Contains)
                   .ForEach(a => _accounts.Accounts.Add(a));
        }

        public static void SetText([NotNull] InputSetting setting)
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

        #endregion
    }
}
