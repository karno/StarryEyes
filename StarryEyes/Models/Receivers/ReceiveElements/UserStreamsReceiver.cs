using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels;
using StarryEyes.Anomaly.TwitterApi.Streaming;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Statuses;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    /// <summary>
    /// Provides connecting User Streams.
    /// </summary>
    /// <remarks>
    /// This class includes error handling strategy.
    /// </remarks>
    public sealed class UserStreamsReceiver : IDisposable
    {
        public const int MaxTrackingKeywordBytes = 60;
        public const int MaxTrackingKeywordCounts = 100;
        private const int HardErrorRetryMaxCount = 3;

        private bool _isEnabled;
        private string[] _trackKeywords = new string[0];
        private UserStreamsConnectionState _state = UserStreamsConnectionState.Disconnected;

        private readonly TwitterAccount _account;
        private CompositeDisposable _currentConnection = new CompositeDisposable();
        private BackOffMode _currentBackOffMode = BackOffMode.None;
        private long _currentBackOffWaitCount;
        private int _hardErrorRetryCount;

        public event Action StateChanged;

        public TwitterAccount Account
        {
            get { return this._account; }
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
                if (_state == value)
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

        public UserStreamsReceiver(TwitterAccount account)
        {
            this._account = account;
        }

        public void Reconnect()
        {
            Debug.WriteLine("*** USER STREAMS RECONNECTING ***");
            if (!IsEnabled)
            {
                Disconnect();
                return;
            }
            CleanupConnection();
            Task.Run(() => _currentConnection.Add(ConnectCore()));
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
            ConnectionState = UserStreamsConnectionState.Connecting;
            var con = this.Account.ConnectUserStreams(this._trackKeywords, this.Account.IsReceiveRepliesAll)
                          .Do(_ =>
                          {
                              if (this.ConnectionState != UserStreamsConnectionState.Connecting) return;
                              this.ConnectionState = UserStreamsConnectionState.Connected;
                              this._currentBackOffMode = BackOffMode.None;
                          })
                          .SubscribeWithHandler(new HandleStreams(this),
                                                this.HandleException,
                                                this.Reconnect);
            return con;
        }

        class HandleStreams : IStreamHandler
        {
            private readonly UserStreamsReceiver _parent;

            public HandleStreams(UserStreamsReceiver parent)
            {
                _parent = parent;
            }

            public void OnStatus(TwitterStatus status)
            {
                StatusInbox.Queue(status);
            }

            public void OnDeleted(StreamDelete item)
            {
                StatusInbox.QueueRemoval(item.Id);
            }

            public void OnDisconnect(StreamDisconnect streamDisconnect)
            {
                BackstageModel.RegisterEvent(new UserStreamsDisconnectedEvent(_parent.Account, streamDisconnect.Reason));
            }

            public void OnEnumerationReceived(StreamEnumeration item)
            {
            }

            public void OnListActivity(StreamListActivity item)
            {
                // TODO: Implementation
            }

            public void OnStatusActivity(StreamStatusActivity item)
            {
                switch (item.Event)
                {
                    case StreamStatusActivityEvent.Unknown:
                        BackstageModel.RegisterEvent(new UnknownEvent(item.Source, item.EventRawString));
                        break;
                    case StreamStatusActivityEvent.Favorite:
                        TwitterEventService.NotifyFavorited(item.Source, item.Status);
                        break;
                    case StreamStatusActivityEvent.Unfavorite:
                        TwitterEventService.NotifyUnfavorited(item.Source, item.Status);
                        break;
                }
            }

            public void OnTrackLimit(StreamTrackLimit item)
            {
                TwitterEventService.NotifyLimitationInfoGot(_parent.Account, (int)item.UndeliveredCount);
            }

            public void OnUserActivity(StreamUserActivity item)
            {
                var active = item.Source.Id == _parent.Account.Id;
                var passive = item.Target.Id == _parent.Account.Id;
                var reldata = _parent.Account.RelationData;
                switch (item.Event)
                {
                    case StreamUserActivityEvent.Unknown:
                        BackstageModel.RegisterEvent(new UnknownEvent(item.Source, item.EventRawString));
                        break;
                    case StreamUserActivityEvent.Follow:
                        if (active)
                        {
                            reldata.AddFollowing(item.Target.Id);
                        }
                        if (passive)
                        {
                            reldata.AddFollower(item.Source.Id);
                        }
                        TwitterEventService.NotifyFollowed(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Unfollow:
                        if (active)
                        {
                            reldata.RemoveFollowing(item.Target.Id);
                        }
                        if (passive)
                        {
                            reldata.RemoveFollower(item.Source.Id);
                        }
                        TwitterEventService.NotifyUnfollwed(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Block:
                        if (active)
                        {
                            reldata.RemoveFollowing(item.Target.Id);
                            reldata.RemoveFollower(item.Target.Id);
                            reldata.AddBlocking(item.Target.Id);
                        }
                        if (passive)
                        {
                            reldata.RemoveFollower(item.Target.Id);
                        }
                        TwitterEventService.NotifyBlocked(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Unblock:
                        if (active)
                        {
                            reldata.RemoveBlocking(item.Target.Id);
                        }
                        TwitterEventService.NotifyUnblocked(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.UserUpdate:
                        TwitterEventService.NotifyUserUpdated(item.Source);
                        break;
                }
            }
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
            var discone = new UserStreamsDisconnectedEvent(this.Account, header + " - " + detail);
            BackstageModel.RegisterEvent(discone);
            ConnectionState = UserStreamsConnectionState.WaitForReconnection;
            _currentConnection.Add(
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .Do(_ => BackstageModel.RemoveEvent(discone))
                          .Subscribe(_ => Reconnect()));
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
            if (!disposing) return;
            this.IsEnabled = false;
            this.StateChanged = null;
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
        Invalid,
        Disconnected,
        Connecting,
        Connected,
        WaitForReconnection,
    }
}
