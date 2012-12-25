using System;
using System.Collections.Generic;
using StarryEyes.Filters.Expressions;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using System.Reactive.Disposables;

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

        public static event Action<TimelineActionRequest> OnTimelineActionRequested;
        public static void ExecuteTimelineAction(TimelineActionRequest req)
        {
            var handler = OnTimelineActionRequested;
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

        public static void ConfirmMute(string description, FilterExpressionBase addExpr)
        {
        }
    }

    public enum AccountSelectionAction
    {
        Favorite,
        Retweet,
    }

    public enum FocusRequest
    {
        Tweet,
        Timeline,
        Find,
    }

    public enum TimelineFocusRequest
    {
        LeftColumn,
        RightColumn,
        LeftTab,
        RightTab,
        TopOfTimeline,
        AboveStatus,
        BelowStatus,
    }

    public enum TimelineActionRequest
    {
        ToggleSelect,
        ClearSelect,
        Reply,
        Favorite,
        Retweet,
        Quote,
        DirectMessage,
        CopyText,
        CopySTOT,
        CopyWebUrl,
        ShowInTwitterWeb,
        ShowUserInfo,
        Delete,
        ReportAsSpam,
        Mute,
    }

}
