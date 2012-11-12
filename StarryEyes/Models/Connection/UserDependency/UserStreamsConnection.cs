using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using StarryEyes.Breezy.Api.Streaming;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Store;
using StarryEyes.Models.Hub;
using StarryEyes.Models.Backpanels;
using StarryEyes.Models.Backpanels.TwitterEvents;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Connection.UserDependency
{
    public sealed class UserStreamsConnection : ConnectionBase
    {
        public const int MaxTrackingKeywordBytes = 60;
        public const int MaxTrackingKeywordCounts = 100;

        const int HardErrorRetryMaxCount = 3;
        int hardErrorRetryCount = 0;

        private enum BackOffMode
        {
            None,
            Network,
            Protocol,
        }

        public event Action<bool> IsConnectionAliveEvent;

        private BackOffMode currentBackOffMode = BackOffMode.None;
        private long currentBackOffWaitCount = 0;

        public UserStreamsConnection(AuthenticateInfo ai) : base(ai) { }
        private IDisposable _connection = null;

        private string[] trackKeywords;
        public IEnumerable<string> TrackKeywords
        {
            get { return trackKeywords; }
            set { trackKeywords = value.ToArray(); }
        }

        /// <summary>
        /// User Streams is connected
        /// </summary>
        public bool IsConnected
        {
            get { return _connection != null; }
        }

        /// <summary>
        /// Connect to user streams.<para />
        /// Or, update connected streams.
        /// </summary>
        public void Connect()
        {
            CheckDisposed();
            Disconnect();
            _connection = this.AuthInfo.ConnectToUserStreams(trackKeywords)
                .Do(_ => currentBackOffMode = BackOffMode.None) // initialize back-off
                .Subscribe(
                _ => Register(_),
                ex => HandleException(ex),
                () =>
                {
                    if (_connection != null)
                    {
                        // make reconnect.
                        Disconnect();
                        System.Diagnostics.Debug.WriteLine("***Auto reconnect***");
                        Connect();
                    }
                });
            RaiseIsConnectedEvent(true);
        }

        /// <summary>
        /// Disconnect from user streams.
        /// </summary>
        public void Disconnect()
        {
            if (_connection != null)
            {
                var disposal = _connection;
                _connection = null;
                disposal.Dispose();
                RaiseIsConnectedEvent(false);
            }
        }

        private void Register(TwitterStreamingElement elem)
        {
            hardErrorRetryCount = 0; // initialize error count
            switch (elem.EventType)
            {
                case EventType.Undefined:
                    // deliver tweet or something.
                    if (elem.Status != null)
                    {
                        StatusStore.Store(elem.Status);
                        // notify status
                        if (elem.Status.RetweetedOriginal != null &&
                            AccountsStore.AccountIds.Contains(elem.Status.RetweetedOriginal.User.Id) &&
                            !Setting.Muteds.Evaluator(elem.Status) &&
                            (!Setting.ApplyMuteToRetweetOriginals.Value || !Setting.Muteds.Evaluator(elem.Status.RetweetedOriginal)))
                        {
                            BackpanelModel.RegisterEvent(new RetweetedEvent(elem.Status.User, elem.Status.RetweetedOriginal));
                        }
                    }
                    if (elem.DeletedId != null)
                        StatusStore.Remove(elem.DeletedId.Value);
                    break;
                case EventType.Follow:
                case EventType.Unfollow:
                    var source = elem.EventSourceUser.Id;
                    var target = elem.EventTargetUser.Id;
                    bool isFollowed = elem.EventType == EventType.Follow;
                    if (source == AuthInfo.Id) // follow or remove
                    {
                        AuthInfo.GetAccountData().SetFollowing(target, isFollowed);
                    }
                    else if (target == AuthInfo.Id) // followed or removed
                    {
                        AuthInfo.GetAccountData().SetFollower(source, isFollowed);
                    }
                    else
                    {
                        return;
                    }
                    if (isFollowed)
                        RegisterEvent(elem);
                    break;
                case EventType.Blocked:
                    if (elem.EventSourceUser.Id != AuthInfo.Id) return;
                    AuthInfo.GetAccountData().AddBlocking(elem.EventTargetUser.Id);
                    break;
                case EventType.Unblocked:
                    if (elem.EventSourceUser.Id != AuthInfo.Id) return;
                    AuthInfo.GetAccountData().RemoveBlocking(elem.EventTargetUser.Id);
                    break;
                default:
                    RegisterEvent(elem);
                    break;
            }
        }

        private void RegisterEvent(TwitterStreamingElement elem)
        {
            BackpanelEventBase ev = null;
            switch(elem.EventType)
            {
                case EventType.Blocked:
                    ev = new BlockedEvent(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Unblocked:
                    ev = new UnblockedEvent(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Favorite:
                    ev = new FavoritedEvent(elem.EventSourceUser, elem.EventTargetTweet);
                    break;
                case EventType.Unfavorite:
                    ev = new UnfavoritedEvent(elem.EventSourceUser, elem.EventTargetTweet);
                    break;
                case EventType.Follow:
                    ev = new FollowedEvent(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Unfollow:
                    ev = new UnfollowedEvent(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.LimitationInfo:
                    ev = new TrackLimitEvent(this.AuthInfo, elem.TrackLimit.GetValueOrDefault());
                    break;
            }
            if (ev != null)
            {
                BackpanelModel.RegisterEvent(ev);
            }
        }

        private void HandleException(Exception ex)
        {
            Disconnect();
            var wex = ex as WebException;
            if (wex != null)
            {
                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    var res = wex.Response as HttpWebResponse;
                    if (res != null)
                    {
                        // protocol error
                        switch (res.StatusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                                // ERR: Unauthorized, invalid OAuth request?
                                if (CheckHardError())
                                {
                                    RaiseDisconnectedByError("ユーザー認証が行えません。",
                                        "PCの時刻設定が正しいか確認してください。回復しない場合は、OAuth認証を再度行ってください。");
                                    return;
                                }
                                break;
                            case HttpStatusCode.Forbidden:
                            case HttpStatusCode.NotFound:
                                if (CheckHardError())
                                {
                                    RaiseDisconnectedByError("ユーザーストリーム接続が一時的、または恒久的に利用できなくなっています。",
                                        "エンドポイントへの接続時にアクセスが拒否されたか、またはエンドポイントが削除されています。");
                                    return;
                                }
                                break;
                            case HttpStatusCode.NotAcceptable:
                            case HttpStatusCode.RequestEntityTooLarge:
                                RaiseDisconnectedByError("トラックしているキーワードが長すぎるか、不正な可能性があります。",
                                    "(トラック中のキーワード:" + trackKeywords.JoinString(", ") + ")");
                                return;
                            case HttpStatusCode.RequestedRangeNotSatisfiable:
                                RaiseDisconnectedByError("ユーザーストリームに接続できません。",
                                    "(テクニカル エラー: 416 Range Unacceptable. Elevated permission is required or paramter is out of range.)");
                                return;
                            case (HttpStatusCode)420:
                                // ERR: Too many connections
                                // (other client is already connected?)
                                if (CheckHardError())
                                {
                                    RaiseDisconnectedByError("ユーザーストリーム接続が制限されています。",
                                        "Krileが多重起動していないか確認してください。短時間に何度も接続を試みていた場合は、しばらく待つと再接続できるようになります。");
                                    return;
                                }
                                break;
                        }
                    }
                    // else -> backoff
                    if (currentBackOffMode == BackOffMode.Protocol)
                        currentBackOffWaitCount += currentBackOffWaitCount; // wait count is raised exponentially.
                    else
                        currentBackOffWaitCount = 5000;
                    if (currentBackOffWaitCount >= 320000) // max wait is 320 sec.
                    {
                        RaiseDisconnectedByError("Twitterが不安定な状態になっています。",
                            "プロトコル エラーにより、ユーザーストリームに既定のリトライ回数内で接続できませんでした。");
                        return;
                    }
                }
                else
                {
                    // network error
                    // -> backoff
                    if (currentBackOffMode == BackOffMode.Network)
                        currentBackOffMode += 250; // wait count is raised linearly.
                    else
                        currentBackOffWaitCount = 250; // wait starts 250ms
                    if (currentBackOffWaitCount >= 16000) // max wait is 16 sec.
                    {
                        RaiseDisconnectedByError("Twitterが不安定な状態になっています。",
                            "ネットワーク エラーにより、ユーザーストリームに規定のリトライ回数内で接続できませんでした。");
                        return;
                    }
                }
            }
            else
            {
                currentBackOffMode = BackOffMode.None;
                if (currentBackOffWaitCount == 110)
                {
                    RaiseDisconnectedByError("ユーザーストリーム接続が何らかのエラーの頻発で停止しました。",
                        "Twitterが不安定な状態になっているか、仕様が変更された可能性があります: " + ex.Message);
                    return;
                }
                if (currentBackOffWaitCount >= 108 && currentBackOffWaitCount <= 109)
                {
                    currentBackOffWaitCount++;
                }
                else
                {
                    currentBackOffWaitCount = 108; // wait shortly
                }
            }
            System.Diagnostics.Debug.WriteLine("*** USER STREAMS error ***" + Environment.NewLine + ex);
            System.Diagnostics.Debug.WriteLine(" -> reconnect.");
            // parsing error, auto-reconnect
            Observable.Timer(TimeSpan.FromMilliseconds(currentBackOffWaitCount))
                .Subscribe(_ => Connect());
        }

        private bool CheckHardError()
        {
            hardErrorRetryCount++;
            if (hardErrorRetryCount > HardErrorRetryMaxCount)
                return true;
            else
                return false;
        }

        private void RaiseIsConnectedEvent(bool connected)
        {
            var handler = IsConnectionAliveEvent;
            if (handler != null)
                handler(connected);
        }

        private void RaiseDisconnectedByError(string header, string detail)
        {
            this.RaiseErrorNotification("UserStreams_Reconnection_" + AuthInfo.UnreliableScreenName,
                header, detail,
                "再接続", () =>
                {
                    if (!IsDisposed)
                        Connect();
                });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Disconnect();
        }
    }
}
