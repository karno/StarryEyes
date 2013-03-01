using System;
using Livet;
using StarryEyes.Models;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public abstract class PartialFlipViewModelBase : ViewModel
    {
        public event Action OnClosed;

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                RaisePropertyChanged(() => IsVisible);
            }
        }

        protected virtual bool IsWindowCommandsRelated
        {
            get { return true; }
        }

        public void Open()
        {
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(false);
            }
            this.Messenger.Raise(new GoToStateMessage("Open"));
        }

        public void Close()
        {
            this.Messenger.Raise(new GoToStateMessage("Close"));
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(true);
            }
            var handler = OnClosed;
            if (handler != null)
                handler();
        }
    }
}
