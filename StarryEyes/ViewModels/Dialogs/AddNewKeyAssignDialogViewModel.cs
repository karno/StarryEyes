using System;
using System.IO;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging.Windows;
using StarryEyes.Globalization.Dialogs;
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
            get => _fileName;
            set
            {
                _fileName = value;
                CheckPathIsValid();
                RaisePropertyChanged(() => IsAcceptOk);
            }
        }

        private void CheckPathIsValid()
        {
            try
            {
                if (String.IsNullOrEmpty(FileName))
                {
                    ErrorMessage = AddNewKeyAssignWindowResources.ErrorFileNameIsEmpty;
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
                    ErrorMessage = AddNewKeyAssignWindowResources.ErrorFileNameIsDuplicated;
                    IsAcceptOk = false;
                    return;
                }
                ErrorMessage = null;
                IsAcceptOk = true;
            }
            catch
            {
                ErrorMessage = AddNewKeyAssignWindowResources.ErrorFileNameContainsInvalidChars;
                IsAcceptOk = false;
            }
        }

        public bool IsAcceptOk
        {
            get => _isAcceptOk;
            set
            {
                _isAcceptOk = value;
                RaisePropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                RaisePropertyChanged();
            }
        }

        [UsedImplicitly]
        public void Ok()
        {
            Result = true;
            Close();
        }

        [UsedImplicitly]
        public void Cancel()
        {
            Result = false;
            Close();
        }

        private void Close()
        {
            Messenger.RaiseSafe(() => new WindowActionMessage(WindowAction.Close));
        }
    }
}