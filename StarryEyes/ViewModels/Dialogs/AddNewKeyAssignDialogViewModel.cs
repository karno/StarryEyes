using System;
using System.IO;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.Dialogs
{
    public class AddNewKeyAssignDialogViewModel : ViewModel
    {
        private string _fileName;
        private string _errorMessage;
        private bool _isAcceptOk;

        public AddNewKeyAssignDialogViewModel()
        {

        }

        public bool Result { get; private set; }

        public bool IsCreateAsCopy { get; set; }

        public string FileName
        {
            get { return this._fileName; }
            set
            {
                this._fileName = value;
                this.CheckPathIsValid();
                RaisePropertyChanged(() => IsAcceptOk);
            }
        }

        private void CheckPathIsValid()
        {
            try
            {
                if (String.IsNullOrEmpty(FileName))
                {
                    ErrorMessage = "ファイル名が入力されていません。";
                    IsAcceptOk = false;
                    return;
                }
                if (FileName.Contains(".") || FileName.Contains("\\"))
                {
                    throw new ArgumentException();
                }
                var fi = new FileInfo(Path.Combine(KeyAssignManager.KeyAssignsProfileDirectoryPath, FileName));
                if (fi.Exists)
                {
                    ErrorMessage = "同じ名前のキーアサインがすでに存在します。";
                    IsAcceptOk = false;
                    return;
                }
                ErrorMessage = null;
                IsAcceptOk = true;
            }
            catch
            {
                ErrorMessage = "使用できない文字が含まれています。";
                IsAcceptOk = false;
            }
        }

        public bool IsAcceptOk
        {
            get { return this._isAcceptOk; }
            set
            {
                this._isAcceptOk = value;
                RaisePropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get { return this._errorMessage; }
            set
            {
                this._errorMessage = value;
                RaisePropertyChanged();
            }
        }

        public void Ok()
        {
            Result = true;
            Close();
        }

        public void Cancel()
        {
            Result = false;
            Close();
        }

        private void Close()
        {
            this.Messenger.Raise(new WindowActionMessage(WindowAction.Close));
        }
    }
}
