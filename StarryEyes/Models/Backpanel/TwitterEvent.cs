using StarryEyes.Models.Store;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Backpanel
{
    /// <summary>
    /// Twitter イベント
    /// </summary>
    public class TwitterEvent : GeneralEvent
    {
        public static GeneralEvent FromStreamingElement(long sourceConnectionUserId, TwitterStreamingElement elem)
        {
            var kind = ConvertEvent(elem.EventType);
            if (kind.HasValue)
            {
                return new TwitterEvent(elem.EventSourceUser, elem.EventTargetUser, elem.EventTargetTweet, kind.Value);
            }
            else if (elem.EventType == EventType.LimitationInfo)
            {
                var ai = AccountsStore.GetAccountSetting(sourceConnectionUserId);
                if (ai == null)
                    return null;
                else
                    return new TwitterLimitationInfo(ai.AuthenticateInfo, elem.TrackLimit.Value);
            }
            else
            {
                return null;
            }
        }

        private static EventKind? ConvertEvent(EventType type)
        {
            switch (type)
            {
                case EventType.Follow:
                    return EventKind.Followed;
                case EventType.Unfollow:
                    return EventKind.Unfollowed;
                case EventType.Favorite:
                    return EventKind.Favorited;
                case EventType.Unfavorite:
                    return EventKind.Unfavorited;
                case EventType.Blocked:
                    return EventKind.Blocked;
                case EventType.Unblocked:
                    return EventKind.Unblocked;
                default:
                    return null;
            }
        }

        private readonly TwitterUser _sourceUser;
        public TwitterUser SourceUser
        {
            get { return _sourceUser; }
        }

        private readonly TwitterUser _targetUser;
        public TwitterUser TargetUser
        {
            get { return _targetUser; }
        }

        private readonly TwitterStatus _targetStatus;
        public TwitterStatus TargetStatus
        {
            get { return _targetStatus; }
        }

        private readonly EventKind _eventKind;
        public override EventKind EventKind
        {
            get { return _eventKind; }
        }

        public TwitterEvent(TwitterUser sourceUser, TwitterUser targetUser, TwitterStatus targetStatus, EventKind eventKind)
        {
            this._sourceUser = sourceUser;
            this._targetUser = targetUser;
            this._targetStatus = targetStatus;
            this._eventKind = eventKind;
        }

        public override string Detail
        {
            get
            {
                if (TargetStatus != null)
                {
                    return SourceUser.ScreenName + " -> " + TargetStatus.ToString();
                }
                else
                {
                    return SourceUser.ScreenName + " -> " + TargetUser.ScreenName;
                }
            }
        }
    }

    public class TwitterLimitationInfo : GeneralEvent
    {
        private readonly AuthenticateInfo _sourceAuthInfo;
        public AuthenticateInfo SourceAuthInfo
        {
            get { return _sourceAuthInfo; }
        }


        private readonly int _skippedCounts;
        public int SkippedCounts
        {
            get { return _skippedCounts; }
        }

        public TwitterLimitationInfo(AuthenticateInfo ai, int skippeds)
        {
            this._sourceAuthInfo = ai;
            this._skippedCounts = skippeds;
        }

        public override EventKind EventKind
        {
            get { return Backpanel.EventKind.TrackLimited; }
        }

        public override string Detail
        {
            get
            {
                return SourceAuthInfo.UnreliableScreenName + " - タイムラインの速度が速すぎるため， " + _skippedCounts + " 件のツイートを受信できませんでした．";
            }
        }
    }
}
