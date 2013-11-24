﻿using Livet;
using Livet.Messaging;
using StarryEyes.Models;
using StarryEyes.Models.Inputting;
using StarryEyes.Settings;
using StarryEyes.Views.Messaging;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class InputViewModel : ViewModel
    {
        private readonly InputCoreViewModel _inputCoreViewModel;

        private readonly AccountSelectorViewModel _accountSelectorViewModel;

        private readonly PostLimitPredictionViewModel _postLimitPredictionViewModel;

        private bool _isOpening;

        public InputViewModel()
        {
            this.CompositeDisposable.Add(_inputCoreViewModel = new InputCoreViewModel(this));
            this.CompositeDisposable.Add(_accountSelectorViewModel = new AccountSelectorViewModel(this));
            this.CompositeDisposable.Add(_postLimitPredictionViewModel = new PostLimitPredictionViewModel());
        }

        public InputCoreViewModel InputCoreViewModel
        {
            get { return this._inputCoreViewModel; }
        }

        public AccountSelectorViewModel AccountSelectorViewModel
        {
            get { return this._accountSelectorViewModel; }
        }

        public PostLimitPredictionViewModel PostLimitPredictionViewModel
        {
            get { return this._postLimitPredictionViewModel; }
        }

        public bool IsOpening
        {
            get { return _isOpening; }
            set
            {
                if (_isOpening == value) return;
                _isOpening = value;
                RaisePropertyChanged(() => IsOpening);
                Messenger.RaiseAsync(value ? new GoToStateMessage("Open") : new GoToStateMessage("Close"));
            }
        }

        public void OpenInput()
        {
            OpenInput(Setting.RestorePreviousStashed.Value);
        }

        public void OpenInput(bool restorePreviousStashed)
        {
            if (!IsOpening)
            {
                IsOpening = true;
                FocusToTextBox();
                if (restorePreviousStashed && InputModel.InputCore.Drafts.Count > 0)
                {
                    var drafts = InputModel.InputCore.Drafts;
                    var last = drafts[drafts.Count - 1];
                    drafts.RemoveAt(drafts.Count - 1);
                    InputCoreViewModel.InputData = last;
                    Messenger.Raise(new TextBoxSetCaretMessage(InputCoreViewModel.InputData.Text.Length, 0));
                }
            }
            else
            {
                FocusToTextBox();
            }
        }

        public void CloseInput()
        {
            if (!IsOpening) return;
            if (InputCoreViewModel.CheckClearInput())
            {
                IsOpening = false;
            }
            // move focus to timeline
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
        }

        public void FocusToTextBox()
        {
            Messenger.Raise(new InteractionMessage("FocusToTextBox"));
        }


    }
}