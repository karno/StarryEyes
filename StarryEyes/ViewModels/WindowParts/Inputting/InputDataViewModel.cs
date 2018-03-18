using System;
using System.Collections.Generic;
using Livet;
using Livet.Commands;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Inputting;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class InputDataViewModel : ViewModel
    {
        private readonly Action<InputData> _removeHandler;

        public InputDataViewModel(InputCoreViewModel parent,
            InputData info, Action<InputData> removeHandler)
        {
            Parent = parent;
            Model = info;
            _removeHandler = removeHandler;
        }

        public InputCoreViewModel Parent { get; }

        public InputData Model { get; }

        public IEnumerable<TwitterAccount> TwitterAccounts => Model.Accounts;

        public string Text => Model.Text;

        #region WritebackCommand

        private ViewModelCommand _writebackCommand;

        public ViewModelCommand WritebackCommand => _writebackCommand ??
                                                    (_writebackCommand = new ViewModelCommand(Writeback));

        #endregion WritebackCommand

        public void Writeback()
        {
            _removeHandler(Model);
            Parent.InputData = Model;
        }

        public void Remove()
        {
            _removeHandler(Model);
        }
    }
}