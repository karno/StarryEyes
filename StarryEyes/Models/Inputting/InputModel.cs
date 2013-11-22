using System;
using System.Reactive.Disposables;
using Livet.EventListeners;
using StarryEyes.Albireo;

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
            _accounts = new AccountSelectorModel(_core.CurrentInputData);

            // link property changing
            var icmpc = new PropertyChangedEventListener(_core);
            icmpc.RegisterHandler(() => _core.CurrentInputData,
                                (o, e) => _accounts.CurrentInputData = _core.CurrentInputData);
            _disposables.Add(icmpc);
            SetEventPropagation();
        }

        #region input events

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

    }
}
