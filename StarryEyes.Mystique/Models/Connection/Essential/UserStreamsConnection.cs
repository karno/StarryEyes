using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using StarryEyes.SweetLady.Api.Streaming;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Models.Hub;

namespace StarryEyes.Mystique.Models.Connection.Essentials
{
    public sealed class UserStreamsConnection : ConnectionBase
    {
        public const int MaxTrackingKeywordBytes = 60;
        public const int MaxTrackingKeywordCounts = 100;
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
            switch (elem.EventType)
            {
                case EventType.Undefined:
                    // deliver tweet or something.
                    if (elem.Status != null)
                        StatusStore.Store(elem.Status);
                    if (elem.DeletedId != null)
                        StatusStore.Remove(elem.DeletedId.Value);
                    if (elem.TrackLimit != null)
                        this.RaiseInfoNotification(
                            "UserStreams_TrackLimit_" + AuthInfo.UnreliableScreenName,
                            "タイムラインの速度が速すぎます。",
                            "ユーザーストリームの取得漏れが起きています。(" + elem.TrackLimit + " 件)");
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
                        EventStore.Store(elem);// send to event store
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
                    EventStore.Store(elem);
                    break;
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
                                RaiseDisconnectedByError("ユーザー認証が行えません。",
                                    "PCの時刻設定が正しいか確認してください。回復しない場合は、OAuth認証を再度行ってください。");
                                return;
                            case HttpStatusCode.Forbidden:
                            case HttpStatusCode.NotFound:
                                RaiseDisconnectedByError("ユーザーストリーム接続が一時的、または恒久的に利用できなくなっています。",
                                    "エンドポイントへの接続時にアクセスが拒否されたか、またはエンドポイントが削除されています。");
                                return;
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
                                RaiseDisconnectedByError("ユーザーストリーム接続が制限されています。",
                                    "Krileが多重起動していないか確認してください。短時間に何度も接続を試みていた場合は、しばらく待つと再接続できるようになります。");
                                return;
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
                Observable.Timer(TimeSpan.FromMilliseconds(currentBackOffWaitCount))
                    .Subscribe(_ => Connect());
            }
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
