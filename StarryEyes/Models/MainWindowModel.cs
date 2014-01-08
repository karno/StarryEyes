using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using StarryEyes.Albireo;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    public static class MainWindowModel
    {
        public static bool SuppressCloseConfirmation { get; set; }

        public static bool SuppressKeyAssigns { get; set; }

        static MainWindowModel()
        {
            App.UserInterfaceReady += () =>
            {
                _isUserInterfaceReady = true;
                RegisterKeyAssigns();
                var handler = TaskDialogRequested;
                if (handler == null) return;
                TaskDialogOptions options;
                while (_taskDialogQueue.TryDequeue(out options))
                {
                    handler(options);
                }
            };
        }

        public static event Action<bool> WindowCommandsDisplayChanged;
        public static void SetShowMainWindowCommands(bool show)
        {
            WindowCommandsDisplayChanged.SafeInvoke(show);
        }

        private static void RegisterKeyAssigns()
        {
            // Focus
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("FocusToTimeline", () => SetFocusTo(FocusRequest.Timeline)),
                KeyAssignAction.Create("FocusToInput", () => SetFocusTo(FocusRequest.Input)),
                KeyAssignAction.Create("FocusToSearch", () => SetFocusTo(FocusRequest.Search)));

            // Timeline move
            KeyAssignManager.RegisterActions(
                KeyAssignAction.Create("SelectLeftColumn", () => SetTimelineFocusTo(TimelineFocusRequest.LeftColumn)),
                KeyAssignAction.Create("SelectRightColumn", () => SetTimelineFocusTo(TimelineFocusRequest.RightColumn)),
                KeyAssignAction.Create("SelectLeftTab", () => SetTimelineFocusTo(TimelineFocusRequest.LeftTab)),
                KeyAssignAction.Create("SelectRightTab", () => SetTimelineFocusTo(TimelineFocusRequest.RightTab)),
                KeyAssignAction.Create("MoveUp", () => SetTimelineFocusTo(TimelineFocusRequest.AboveStatus)),
                KeyAssignAction.Create("MoveDown", () => SetTimelineFocusTo(TimelineFocusRequest.BelowStatus)),
                KeyAssignAction.Create("MoveTop", () => SetTimelineFocusTo(TimelineFocusRequest.TopOfTimeline)),
                KeyAssignAction.Create("MoveBottom", () => SetTimelineFocusTo(TimelineFocusRequest.BottomOfTimeline)));

        }

        #region Focus and timeline action control

        public static event Action<FocusRequest> FocusRequested;

        public static void SetFocusTo(FocusRequest req)
        {
            FocusRequested.SafeInvoke(req);
        }

        public static event Action<TimelineFocusRequest> TimelineFocusRequested;

        public static void SetTimelineFocusTo(TimelineFocusRequest req)
        {
            TimelineFocusRequested.SafeInvoke(req);
        }

        #endregion

        public static event Action<AccountSelectDescription> AccountSelectActionRequested;
        public static void ExecuteAccountSelectAction(
            AccountSelectionAction action, IEnumerable<TwitterAccount> defaultSelected,
            Action<IEnumerable<TwitterAccount>> after)
        {
            ExecuteAccountSelectAction(new AccountSelectDescription
            {
                AccountSelectionAction = action,
                SelectionAccounts = defaultSelected,
                Callback = after
            });
        }

        public static void ExecuteAccountSelectAction(AccountSelectDescription desc)
        {
            AccountSelectActionRequested.SafeInvoke(desc);
        }

        private static readonly LinkedList<string> _stateStack = new LinkedList<string>();
        public static event Action StateStringChanged;

        public static string StateString
        {
            get
            {
                var item = _stateStack.First;
                if (item != null)
                {
                    return item.Value;
                }
                return App.DefaultStatusMessage;
            }
        }

        public static IDisposable SetState(string state)
        {
            var node = _stateStack.AddFirst(state);
            RaiseStateStringChanged();
            return Disposable.Create(() =>
            {
                _stateStack.Remove(node);
                RaiseStateStringChanged();
            });
        }

        private static void RaiseStateStringChanged()
        {
            StateStringChanged.SafeInvoke();
        }

        public static event Action<Tuple<TabModel, ISubject<Unit>>> TabConfigureRequested;
        public static IObservable<Unit> ShowTabConfigure(TabModel model)
        {
            var notifier = new Subject<Unit>();
            var handler = TabConfigureRequested;
            if (handler != null)
            {
                handler(Tuple.Create(model, (ISubject<Unit>)notifier));
            }
            else
            {
                notifier.OnCompleted();
            }
            return notifier;
        }

        public static event Action<ISubject<Unit>> SettingRequested;
        public static IObservable<Unit> ShowSetting()
        {
            var notifier = new Subject<Unit>();
            var handler = SettingRequested;
            if (handler != null)
            {
                handler(notifier);
            }
            else
            {
                notifier.OnCompleted();
            }
            return notifier;
        }

        public static event Action<bool> BackstageTransitionRequested;

        public static void TransitionBackstage(bool open)
        {
            BackstageTransitionRequested.SafeInvoke(open);
        }

        private static volatile bool _isUserInterfaceReady;
        private static readonly ConcurrentQueue<TaskDialogOptions> _taskDialogQueue = new ConcurrentQueue<TaskDialogOptions>();
        public static event Action<TaskDialogOptions> TaskDialogRequested;

        public static void ShowTaskDialog(TaskDialogOptions options)
        {
            if (!_isUserInterfaceReady)
            {
                _taskDialogQueue.Enqueue(options);
            }
            TaskDialogRequested.SafeInvoke(options);
        }
    }

    public class StateUpdater
    {
        private IDisposable _disposable;

        public void UpdateState(string status = null)
        {
            IDisposable nd = null;
            if (!String.IsNullOrEmpty(status))
            {
                nd = MainWindowModel.SetState(status);
            }
            var pd = Interlocked.Exchange(ref _disposable, nd);
            if (pd != null)
            {
                pd.Dispose();
            }
        }
    }

    public class AccountSelectDescription
    {
        public AccountSelectionAction AccountSelectionAction { get; set; }

        public IEnumerable<TwitterAccount> SelectionAccounts { get; set; }

        public Action<IEnumerable<TwitterAccount>> Callback { get; set; }
    }

    public enum AccountSelectionAction
    {
        Favorite,
        Retweet,
    }

    public enum FocusRequest
    {
        Input,
        Timeline,
        Search,
    }

    public enum TimelineFocusRequest
    {
        LeftColumn,
        RightColumn,
        LeftTab,
        RightTab,
        TopOfTimeline,
        BottomOfTimeline,
        AboveStatus,
        BelowStatus,
    }

    public enum TimelineActionRequest
    {
        ToggleFocus,
        ToggleSelect,
        ClearSelect,
        Reply,
        Favorite,
        FavoriteMultiple,
        Retweet,
        RetweetMultiple,
        Quote,
        QuoteLink,
        DirectMessage,
        CopyText,
        CopySTOT,
        CopyPermalink,
        ShowConversation,
        ShowTweetInTwitterWeb,
        ShowTweetInFavstar,
        ShowUserInTwitterWeb,
        ShowUserInFavstar,
        ShowUserInTwilog,
        ShowUserInfo,
        GiveTrophy,
        Delete,
        ReportAsSpam,
        MuteKeyword,
        MuteUser,
        MuteClient,
    }
}
