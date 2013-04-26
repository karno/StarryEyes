using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Breezy.Api.Streaming;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public sealed class UserStreamsReceiver : IDisposable
    {
        public const int MaxTrackingKeywordBytes = 60;
        public const int MaxTrackingKeywordCounts = 100;
        private const int HardErrorRetryMaxCount = 3;

        private bool _isEnabled;
        private string[] _trackKeywords = new string[0];
        private UserStreamsConnectionState _state;

        private readonly AuthenticateInfo _authInfo;
        private CompositeDisposable _currentConnection = new CompositeDisposable();
        private BackOffMode _currentBackOffMode = BackOffMode.None;
        private long _currentBackOffWaitCount;
        private int _hardErrorRetryCount;

        public event Action StateChanged;

        public AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                _isEnabled = value;
                if (!value)
                {
                    Disconnect();
                }
                else if (ConnectionState == UserStreamsConnectionState.Disconnected ||
                         ConnectionState == UserStreamsConnectionState.WaitForReconnection)
                {
                    Reconnect();
                }
            }
        }

        public IEnumerable<string> TrackKeywords
        {
            get { return _trackKeywords ?? Enumerable.Empty<string>(); }
            set
            {
                var prev = _trackKeywords;
                _trackKeywords = (value ?? Enumerable.Empty<string>()).ToArray();
                if (prev.Length != _trackKeywords.Length ||
                    prev.Any(s => !_trackKeywords.Contains(s)))
                {
                    // change keywords list
                    Reconnect();
                }
            }
        }

        public UserStreamsConnectionState ConnectionState
        {
            get { return _state; }
            private set
            {
                if (_state != value)
                {
                    return;
                }
                _state = value;
                var handler = StateChanged;
                if (handler != null)
                {
                    handler();
                }
            }
        }

        public UserStreamsReceiver(AuthenticateInfo authInfo)
        {
            _authInfo = authInfo;
        }

        public void Reconnect()
        {
            System.Diagnostics.Debug.WriteLine("*** USER STREAMS RECONNECTING ***");
            if (!IsEnabled)
            {
                Disconnect();
                return;
            }
            CleanupConnection();
            ConnectionState = UserStreamsConnectionState.Connecting;
            _currentConnection.Add(ConnectCore());
        }

        private void Disconnect()
        {
            ConnectionState = UserStreamsConnectionState.Disconnected;
            CleanupConnection();
        }

        private void CleanupConnection()
        {
            Interlocked.Exchange(ref _currentConnection, new CompositeDisposable())
                .Dispose();
        }

        private IDisposable ConnectCore()
        {
            CheckDisposed();
            return AuthInfo.ConnectToUserStreams(_trackKeywords)
                           .Do(_ =>
                           {
                               ConnectionState = UserStreamsConnectionState.Connected;
                               _currentBackOffMode = BackOffMode.None;
                           })
                           .Subscribe(
                               Register,
                               HandleException,
                               Reconnect);
        }

        #region Error handlers

        private void HandleException(Exception ex)
        {
            CleanupConnection();
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
                                    RaiseDisconnectedByError(
                                        "ユーザー認証が行えません。",
                                        "PCの時刻設定が正しいか確認してください。回復しない場合は、OAuth認証を再度行ってください。");
                                    return;
                                }
                                break;
                            case HttpStatusCode.Forbidden:
                            case HttpStatusCode.NotFound:
                                if (CheckHardError())
                                {
                                    RaiseDisconnectedByError(
                                        "ユーザーストリーム接続が一時的、または恒久的に利用できなくなっています。",
                                        "エンドポイントへの接続時にアクセスが拒否されたか、またはエンドポイントが削除されています。");
                                    return;
                                }
                                break;
                            case HttpStatusCode.NotAcceptable:
                            case HttpStatusCode.RequestEntityTooLarge:
                                RaiseDisconnectedByError(
                                    "トラックしているキーワードが長すぎるか、不正な可能性があります。",
                                    "(トラック中のキーワード:" + _trackKeywords.JoinString(", ") + ")");
                                return;
                            case HttpStatusCode.RequestedRangeNotSatisfiable:
                                RaiseDisconnectedByError(
                                    "ユーザーストリームに接続できません。",
                                    "(システム エラー: 416 Range Unacceptable. Elevated permission is required or paramter is out of range.)");
                                return;
                            case (HttpStatusCode)420:
                                // ERR: Too many connections
                                // (other client is already connected?)
                                RaiseDisconnectedByError(
                                    "ユーザーストリーム接続が制限されています。",
                                    "Krileが多重起動していないか確認してください。短時間に何度も接続を試みていた場合は、しばらく待つと再接続できるようになります。");
                                return;
                        }
                    }
                    // else -> backoff
                    if (_currentBackOffMode == BackOffMode.ProtocolError)
                        _currentBackOffWaitCount += _currentBackOffWaitCount; // wait count is raised exponentially.
                    else
                        _currentBackOffWaitCount = 5000;
                    if (_currentBackOffWaitCount >= 320000) // max wait is 320 sec.
                    {
                        RaiseDisconnectedByError(
                            "Twitterが不安定な状態になっています。",
                            "プロトコル エラーにより、ユーザーストリームに既定のリトライ回数内で接続できませんでした。");
                        return;
                    }
                }
                else
                {
                    // network error
                    // -> backoff
                    if (_currentBackOffMode == BackOffMode.NetworkError)
                        _currentBackOffMode += 250; // wait count is raised linearly.
                    else
                        _currentBackOffWaitCount = 250; // wait starts 250ms
                    if (_currentBackOffWaitCount >= 16000) // max wait is 16 sec.
                    {
                        RaiseDisconnectedByError(
                            "Twitterが不安定な状態になっています。",
                            "ネットワーク エラーにより、ユーザーストリームに規定のリトライ回数内で接続できませんでした。");
                        return;
                    }
                }
            }
            else
            {
                _currentBackOffMode = BackOffMode.None;
                if (_currentBackOffWaitCount == 110)
                {
                    RaiseDisconnectedByError(
                        "ユーザーストリーム接続が何らかのエラーの頻発で停止しました。",
                        "Twitterが不安定な状態になっているか、仕様が変更された可能性があります: " + ex.Message);
                    return;
                }
                if (_currentBackOffWaitCount >= 108 && _currentBackOffWaitCount <= 109)
                {
                    _currentBackOffWaitCount++;
                }
                else
                {
                    _currentBackOffWaitCount = 108; // wait shortly
                }
            }
            Debug.WriteLine("*** USER STREAMS error ***" + Environment.NewLine + ex);
            Debug.WriteLine(" -> reconnect.");
            // parsing error, auto-reconnect
            _currentConnection.Add(
                Observable.Timer(TimeSpan.FromMilliseconds(_currentBackOffWaitCount))
                          .Subscribe(_ => Reconnect()));
        }

        private bool CheckHardError()
        {
            _hardErrorRetryCount++;
            if (_hardErrorRetryCount > HardErrorRetryMaxCount)
            {
                return true;
            }
            return false;
        }

        private void RaiseDisconnectedByError(string header, string detail)
        {
            CleanupConnection();
            Debug.WriteLine("*** USER STREAM DISCONNECT ***" + Environment.NewLine + header + Environment.NewLine + detail + Environment.NewLine);
            var discone = new UserStreamsDisconnectedEvent(AuthInfo, header + " - " + detail, this.Reconnect);
            BackstageModel.RegisterEvent(discone);
            ConnectionState = UserStreamsConnectionState.WaitForReconnection;
            _currentConnection.Add(
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .Do(_ => BackstageModel.RemoveEvent(discone))
                          .Subscribe(_ => Reconnect()));
        }

        #endregion

        #region Handle elements

        private void Register(TwitterStreamingElement elem)
        {
            _hardErrorRetryCount = 0; // initialize error count
            switch (elem.EventType)
            {
                case EventType.Empty:
                    // deliver tweet or something.
                    if (elem.Status != null)
                    {
                        ReceiveInbox.Queue(elem.Status);
                    }
                    if (elem.DeletedId != null)
                    {
                        StatusStore.Remove(elem.DeletedId.Value);
                    }
                    break;
                case EventType.Follow:
                case EventType.Unfollow:
                    var source = elem.EventSourceUser.Id;
                    var target = elem.EventTargetUser.Id;
                    var isFollowed = elem.EventType == EventType.Follow;
                    if (source == AuthInfo.Id) // follow or remove
                    {
                        AuthInfo.GetRelationData().SetFollowing(target, isFollowed);
                    }
                    else if (target == AuthInfo.Id) // followed or removed
                    {
                        AuthInfo.GetRelationData().SetFollower(source, isFollowed);
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
                    AuthInfo.GetRelationData().AddBlocking(elem.EventTargetUser.Id);
                    break;
                case EventType.Unblocked:
                    if (elem.EventSourceUser.Id != AuthInfo.Id) return;
                    AuthInfo.GetRelationData().RemoveBlocking(elem.EventTargetUser.Id);
                    break;
                default:
                    RegisterEvent(elem);
                    break;
            }
        }

        private void RegisterEvent(TwitterStreamingElement elem)
        {
            switch (elem.EventType)
            {
                case EventType.Blocked:
                    StreamingEventsHub.NotifyBlocked(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Unblocked:
                    StreamingEventsHub.NotifyUnblocked(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Favorite:
                    StreamingEventsHub.NotifyFavorited(elem.EventSourceUser, elem.EventTargetTweet);
                    break;
                case EventType.Unfavorite:
                    StreamingEventsHub.NotifyUnfavorited(elem.EventSourceUser, elem.EventTargetTweet);
                    break;
                case EventType.Follow:
                    StreamingEventsHub.NotifyFollowed(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.Unfollow:
                    StreamingEventsHub.NotifyUnfollwed(elem.EventSourceUser, elem.EventTargetUser);
                    break;
                case EventType.LimitationInfo:
                    StreamingEventsHub.NotifyLimitationInfoGot(AuthInfo, elem.TrackLimit.GetValueOrDefault());
                    break;
                default:
                    return;
            }
        }

        #endregion

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("UserStreamsReceiver");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~UserStreamsReceiver()
        {
            Dispose(false);
        }

        private bool _disposed;
        private void Dispose(bool disposing)
        {
            _disposed = true;
            if (disposing)
            {
                this.IsEnabled = false;
            }
        }
    }

    enum BackOffMode
    {
        None,
        NetworkError,
        ProtocolError,
    }

    public enum UserStreamsConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        WaitForReconnection,
    }
}
