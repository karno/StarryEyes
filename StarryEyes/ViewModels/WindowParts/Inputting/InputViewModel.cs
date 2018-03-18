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
        private bool _isOpening;

        public InputViewModel()
        {
            CompositeDisposable.Add(InputCoreViewModel = new InputCoreViewModel(this));
            CompositeDisposable.Add(AccountSelectorViewModel = new AccountSelectorViewModel(this));
            CompositeDisposable.Add(PostLimitPredictionViewModel = new PostLimitPredictionViewModel());

            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputModel.FocusRequest += h,
                    h => InputModel.FocusRequest -= h,
                    () =>
                    {
                        OpenInput();
                        FocusToTextBox();
                    }));
            CompositeDisposable.Add(
                new EventListener<Action>(
                    h => InputModel.CloseRequest += h,
                    h => InputModel.CloseRequest -= h,
                    CloseInput));
            RegisterKeyAssigns();
        }

        public InputCoreViewModel InputCoreViewModel { get; }

        public AccountSelectorViewModel AccountSelectorViewModel { get; }

        public PostLimitPredictionViewModel PostLimitPredictionViewModel { get; }

        private void RegisterKeyAssigns()
        {
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("CloseInput", CloseInput),
                KeyAssignAction.Create("Post", InputCoreViewModel.Send),
                KeyAssignAction.Create("LoadStash", () =>
                {
                    if (InputCoreViewModel.IsDraftsExisted)
                    {
                        InputCoreViewModel.DraftedInputs[0].Writeback();
                    }
                }),
                KeyAssignAction.Create("Amend", InputCoreViewModel.AmendLastPosted),
                KeyAssignAction.Create("AttachImage", () => { InputCoreViewModel.AttachImage(); }),
                KeyAssignAction.Create("ToggleEscape",
                    () => { InputCoreViewModel.IsUrlAutoEsacpeEnabled = !InputCoreViewModel.IsUrlAutoEsacpeEnabled; }),
                KeyAssignAction.Create("SelectNextAccount", () => AccountSelectorViewModel.SelectNext()),
                KeyAssignAction.Create("SelectPreviousAccount", () => AccountSelectorViewModel.SelectPrev()),
                KeyAssignAction.Create("ClearSelectedAccounts", () => AccountSelectorViewModel.ClearAll()),
                KeyAssignAction.Create("SelectAllAccounts", () => AccountSelectorViewModel.SelectAll())
            );
        }

        public bool IsOpening
        {
            get => _isOpening;
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
            Messenger.RaiseSafe(() => new InteractionMessage("FocusToTextBox"));
        }
    }
}