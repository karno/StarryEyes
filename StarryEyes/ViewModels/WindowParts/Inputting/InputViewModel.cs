using System;
using Livet;
using Livet.EventListeners;
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

            this.CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputModel.FocusRequest += h,
                    h => InputModel.FocusRequest -= h,
                    () =>
                    {
                        OpenInput();
                        FocusToTextBox();
                    }));
            this.CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputModel.CloseRequest += h,
                    h => InputModel.CloseRequest -= h,
                    CloseInput));
            this.RegisterKeyAssigns();
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

        private void RegisterKeyAssigns()
        {
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("CloseInput", this.CloseInput),
                KeyAssignAction.Create("Post", this.InputCoreViewModel.Send),
                KeyAssignAction.Create("LoadStash", () =>
                {
                    if (this.InputCoreViewModel.IsDraftsExisted)
                    {
                        this.InputCoreViewModel.DraftedInputs[0].Writeback();
                    }
                }),
                KeyAssignAction.Create("Amend", this.InputCoreViewModel.AmendLastPosted),
                KeyAssignAction.Create("AttachImage", () =>
                {
                    if (this.InputCoreViewModel.IsLocationAttached)
                    {
                        this.InputCoreViewModel.DetachImage();
                    }
                    else
                    {
                        this.InputCoreViewModel.AttachImage();
                    }
                }),
                KeyAssignAction.Create("ToggleEscape", () =>
                {
                    this.InputCoreViewModel.IsUrlAutoEsacpeEnabled = !this.InputCoreViewModel.IsUrlAutoEsacpeEnabled;
                }),
                KeyAssignAction.Create("SelectNextAccount", () => this.AccountSelectorViewModel.SelectNext()),
                KeyAssignAction.Create("SelectPreviousAccount", () => this.AccountSelectorViewModel.SelectPrev()),
                KeyAssignAction.Create("ClearSelectedAccounts", () => this.AccountSelectorViewModel.ClearAll()),
                KeyAssignAction.Create("SelectAllAccounts", () => this.AccountSelectorViewModel.SelectAll())
                );
        }

        public bool IsOpening
        {
            get { return _isOpening; }
            set
            {
                if (_isOpening == value) return;
                _isOpening = value;
                RaisePropertyChanged(() => IsOpening);
                Messenger.RaiseSafe(() => value ? new GoToStateMessage("Open") : new GoToStateMessage("Close"));
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
                    Messenger.RaiseSafe(() => new TextBoxSetCaretMessage(last.Text.Length, 0));
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
                // reset tab-account synchronization
                AccountSelectorViewModel.SynchronizeWithTab();
                // close
                IsOpening = false;
            }
            // move focus to timeline
            MainWindowModel.SetFocusTo(FocusRequest.Timeline);
        }

        public void FocusToTextBox()
        {
            DispatcherHelper.UIDispatcher.VerifyAccess();
            this.Messenger.RaiseSafe(() => new InteractionMessage("FocusToTextBox"));
        }
    }
}
