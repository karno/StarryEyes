using System;
using System.Collections.Generic;
using StarryEyes.Filters.Expressions;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models
{
    public static class MainWindowModel
    {
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

        public static void ExecuteAccountSelectAction(
            AccountSelectionAction action, TwitterStatus targetStatus,
            IEnumerable<AuthenticateInfo> defaultSelected, Action<IEnumerable<AuthenticateInfo>> after)
        {
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
