using System;
using System.Collections.Generic;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Models.Hub
{
    public static class UIHub
    {
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
