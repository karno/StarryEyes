using System.Reactive.Disposables;
using Livet.EventListeners;

namespace StarryEyes.Models.Inputting
{
    public static class InputModel
    {
        private static readonly CompositeDisposable _disposables;

        private static readonly AccountSelectorModel _accountSelectorModel;

        private static readonly InputCoreModel _inputCoreModel;

        static InputModel()
        {
            _disposables = new CompositeDisposable();
            _inputCoreModel = new InputCoreModel();
            _accountSelectorModel = new AccountSelectorModel(InputCoreModel.CurrentInputData);

            // link property changing
            var icmpc = new PropertyChangedEventListener(InputCoreModel);
            icmpc.RegisterHandler(() => InputCoreModel.CurrentInputData,
                                (o, e) => AccountSelectorModel.CurrentInputData = InputCoreModel.CurrentInputData);
            _disposables.Add(icmpc);
        }

        public static AccountSelectorModel AccountSelectorModel
        {
            get { return _accountSelectorModel; }
        }

        public static InputCoreModel InputCoreModel
        {
            get { return _inputCoreModel; }
        }
    }
}
