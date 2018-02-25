using System;
using Livet;
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
            get => _isVisible;
            set
            {
                _isVisible = value;
                RaisePropertyChanged(() => IsVisible);
            }
        }

        protected virtual bool IsWindowCommandsRelated => true;

        public virtual void Open()
        {
            if (IsVisible) return;
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(false);
            }
            IsVisible = true;
            Messenger.RaiseSafe(() => new GoToStateMessage("Open"));
        }

        public virtual void Close()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Messenger.RaiseSafe(() => new GoToStateMessage("Close"));
            if (IsWindowCommandsRelated)
            {
                MainWindowModel.SetShowMainWindowCommands(true);
            }
            Closed?.Invoke();
        }
    }
}