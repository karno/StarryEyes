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
            this.Parent = parent;
            this.Model = info;
            this._removeHandler = removeHandler;
        }

        public InputCoreViewModel Parent { get; private set; }

        public InputData Model { get; private set; }

        public IEnumerable<TwitterAccount> TwitterAccounts
        {
            get { return this.Model.Accounts; }
        }

        public string Text
        {
            get { return this.Model.Text; }
        }

        #region WritebackCommand

        private ViewModelCommand _writebackCommand;

        public ViewModelCommand WritebackCommand
        {
            get { return this._writebackCommand ?? (this._writebackCommand = new ViewModelCommand(this.Writeback)); }
        }

        #endregion

        public void Writeback()
        {
            this._removeHandler(this.Model);
            this.Parent.InputData = this.Model;
        }

        public void Remove()
        {
            this._removeHandler(this.Model);
        }
    }
}