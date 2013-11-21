using System;
using Livet;
using StarryEyes.Albireo;
using StarryEyes.Models;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public abstract class PartialFlipViewModelBase : ViewModel
    {
        public event Action Closed;

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

        public virtual void Open()
        {
            if (IsVisible) return;
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(false);
            }
            IsVisible = true;
            this.Messenger.Raise(new GoToStateMessage("Open"));
        }

        public virtual void Close()
        {
            if (!IsVisible) return;
            IsVisible = false;
            this.Messenger.Raise(new GoToStateMessage("Close"));
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(true);
            }
            this.Closed.SafeInvoke();
        }
    }
}
