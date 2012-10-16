using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarryEyes.Models.Backpanel
{
    /// <summary>
    /// StarryEyes内で発生し，バックパネルへ通知されるイベントの基底クラス
    /// </summary>
    public abstract class GeneralEvent
    {
        public abstract EventKind EventKind { get; }

        public virtual string EventDescription
        {
            get
            {
                switch (EventKind)
                {
                    case Backpanel.EventKind.SystemInformation:
                        return "システム情報";
                    case Backpanel.EventKind.SystemWarning:
                        return "システム警告";
                    case Backpanel.EventKind.SystemError:
                        return "システムエラー";
                    case Backpanel.EventKind.Fallbacked:
                        return "フォールバック";
                    case Backpanel.EventKind.PostSuppressed:
                        return "POST規制";
                    case Backpanel.EventKind.FavoriteSuppressed:
                        return "Favorite規制";
                    case Backpanel.EventKind.Favorited:
                        return "お気に入りに追加";
                    case Backpanel.EventKind.Unfavorited:
                        return "お気に入りを解除";
                    case Backpanel.EventKind.Followed:
                        return "フォロー";
                    case Backpanel.EventKind.Unfollowed:
                        return "アンフォロー";
                    case Backpanel.EventKind.Retweeted:
                        return "リツイート";
                    case Backpanel.EventKind.Blocked:
                        return "ブロック";
                    case Backpanel.EventKind.Unblocked:
                        return "ブロック解除";
                    case Backpanel.EventKind.TrackLimited:
                        return "UserStreams受信制限";
                    default:
                        throw new InvalidOperationException("Invalid event kind");
                }
            }
        }

        public abstract string Detail { get; }

        public virtual bool IsActionEnabled { get { return false; } }

        public virtual void ExecuteAction() { }
    }

    public enum EventKind
    {
        // Systems

        SystemInformation,
        SystemWarning,
        SystemError,

        // Operations

        Fallbacked,
        PostSuppressed,
        FavoriteSuppressed,

        // Twitter Events

        Favorited,
        Unfavorited,
        Followed,
        Unfollowed,
        Retweeted,
        Blocked,
        Unblocked,
        TrackLimited,

    }
}
