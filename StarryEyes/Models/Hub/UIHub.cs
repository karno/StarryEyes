using System;
using System.Collections.Generic;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Models.Hub
{
    public static class UIHub
    {
        public static event Action<IEnumerable<AuthenticateInfo>, IEnumerable<string>> OnTextBindRequested;
        public static void SetTextBind(IEnumerable<AuthenticateInfo> authInfos, IEnumerable<string> bindHashtags)
        {
            var handler = OnTextBindRequested;
            if (handler != null)
                handler(authInfos, bindHashtags);
        }

        public static event Action<IEnumerable<AuthenticateInfo>, string, CursorPosition, TwitterStatus> OnSetTextRequested;
        public static void SetText(IEnumerable<AuthenticateInfo> infos = null, string body = null,
            CursorPosition curpos = CursorPosition.End, TwitterStatus inReplyTo = null, bool focusToInputArea = true)
        {
            if (String.IsNullOrWhiteSpace(body))
                body = String.Empty;

            var handler = OnSetTextRequested;
            if (handler != null)
                handler(infos, body, curpos, inReplyTo);

            if (focusToInputArea)
                SetFocusTo(FocusRequest.Tweet);
        }

        public static event Action<IEnumerable<AuthenticateInfo>, TwitterUser> OnDirectMessageRequested;
        public static void SetDirectMessage(IEnumerable<AuthenticateInfo> infos, TwitterUser recipient, bool focusToInputArea = true)
        {
            var handler = OnDirectMessageRequested;
            if (handler != null)
                handler(infos, recipient);
            if (focusToInputArea)
                SetFocusTo(FocusRequest.Tweet);
        }

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

    public enum CursorPosition
    {
        Begin,
        End,
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

    public enum AccountSelectionAction
    {
        Favorite,
        Retweet,
        FavoriteAndRetweet,
        Stealing,
    }
}
