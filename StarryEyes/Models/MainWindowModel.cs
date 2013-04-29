using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Filters.Expressions;
using StarryEyes.Models.Tab;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    public static class MainWindowModel
    {
        public static event Action<bool> OnWindowCommandDisplayChanged;
        public static void SetShowMainWindowCommands(bool show)
        {
            var handler = OnWindowCommandDisplayChanged;
            if (handler != null)
                handler(show);
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

        public static event Action<FocusRequest> OnFocusRequested;
        public static void SetFocusTo(FocusRequest req)
        {
            var handler = OnFocusRequested;
            if (handler != null)
                handler(req);
        }

        public static event Action<TimelineFocusRequest> OnTimelineFocusRequested;
        public static void SetTimelineFocusTo(TimelineFocusRequest req)
        {
            var handler = OnTimelineFocusRequested;
            if (handler != null)
                handler(req);
        }

        #endregion

        public static event Action<AccountSelectionAction, TwitterStatus, IEnumerable<AuthenticateInfo>, Action<IEnumerable<AuthenticateInfo>>> OnExecuteAccountSelectActionRequested;
        public static void ExecuteAccountSelectAction(
            AccountSelectionAction action, TwitterStatus targetStatus,
            IEnumerable<AuthenticateInfo> defaultSelected, Action<IEnumerable<AuthenticateInfo>> after)
        {
            var handler = OnExecuteAccountSelectActionRequested;
            if (handler != null)
                handler(action, targetStatus, defaultSelected, after);
        }

        private static readonly LinkedList<string> _stateStack = new LinkedList<string>();
        public static event Action OnStateStringChanged;

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
            var handler = OnStateStringChanged;
            if (handler != null) handler();
        }

        public static void ShowUserInfo(TwitterUser user)
        {
        }

        public static event Action<Tuple<string, FilterExpressionBase>> OnConfirmMuteRequested;
        public static void ConfirmMute(string description, FilterExpressionBase addExpr)
        {
            var handler = OnConfirmMuteRequested;
            if (handler != null)
            {
                handler(Tuple.Create(description, addExpr));
            }
        }

        public static event Action<TabModel> OnTabModelConfigureRaised;
        public static void ShowTabConfigure(TabModel model)
        {
            var handler = OnTabModelConfigureRaised;
            if (handler != null) handler(model);
        }

        public static event Action<bool> OnBackstageTransitionRequested;
        public static void TransitionBackstage(bool open)
        {
            var handler = OnBackstageTransitionRequested;
            if (handler != null) handler(open);
        }
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
