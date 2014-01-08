using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Albireo;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels;
using StarryEyes.Anomaly.TwitterApi.Streaming;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Receiving.Receivers
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
        private readonly StateUpdater _stateUpdater;

        private readonly TwitterAccount _account;
        private CompositeDisposable _currentConnection = new CompositeDisposable();

        public event Action StateChanged;

        public TwitterAccount Account
        {
            get { return this._account; }
        }

        public bool IsEnabled
        {
            get { return this._isEnabled; }
            set
            {
                if (this._isEnabled == value)
                {
                    return;
                }
                this._isEnabled = value;
                if (!value)
                {
                    this.Disconnect();
                }
                else if (this.ConnectionState == UserStreamsConnectionState.Disconnected ||
                         this.ConnectionState == UserStreamsConnectionState.WaitForReconnection)
                {
                    this.Reconnect();
                }
            }
        }

        public IEnumerable<string> TrackKeywords
        {
            get { return this._trackKeywords ?? Enumerable.Empty<string>(); }
            set
            {
                var prev = this._trackKeywords;
                this._trackKeywords = (value ?? Enumerable.Empty<string>()).ToArray();
                if (prev.Length != this._trackKeywords.Length ||
                    prev.Any(s => !this._trackKeywords.Contains(s)))
                {
                    // change keywords list
                    this.Reconnect();
                }
            }
        }

        public UserStreamsConnectionState ConnectionState
        {
            get { return this._state; }
            private set
            {
                if (this._state == value)
                {
                    return;
                }
                this._state = value;
                StateChanged.SafeInvoke();
            }
        }

        public UserStreamsReceiver(TwitterAccount account)
        {
            this._stateUpdater = new StateUpdater();
            this._account = account;
        }

        public void Reconnect()
        {
            if (!this.IsEnabled)
            {
                Debug.WriteLine("*USERSTREAMS* disconnect.");
                this.Disconnect();
                return;
            }
            this._stateUpdater.UpdateState(_account.UnreliableScreenName + ": User Streamsを再接続しています...");
            Debug.WriteLine("*USERSTREAMS* Reconnecting " + _account.UnreliableScreenName + " ...");
            this.CleanupConnection();
            Task.Run(() => this._currentConnection.Add(this.ConnectCore()));
        }

        private void Disconnect()
        {
            this.ConnectionState = UserStreamsConnectionState.Disconnected;
            this.CleanupConnection();
        }

        private void CleanupConnection()
        {
            Interlocked.Exchange(ref this._currentConnection, new CompositeDisposable())
                       .Dispose();
        }

        private IDisposable ConnectCore()
        {
            this.CheckDisposed();
            this.ConnectionState = UserStreamsConnectionState.Connecting;
            Debug.WriteLine("*USERSTREAMS* " + _account.UnreliableScreenName + ": Starting connection...");
            var con = this.Account.ConnectUserStreams(this._trackKeywords, this.Account.ReceiveRepliesAll,
                                                      this.Account.ReceiveFollowingsActivity)
                          .Do(_ =>
                          {
                              if (this.ConnectionState != UserStreamsConnectionState.Connecting) return;
                              this.ConnectionState = UserStreamsConnectionState.Connected;
                              this.ResetErrorParams();
                          })
                          .SubscribeWithHandler(new HandleStreams(this),
                                                this.HandleException,
                                                this.Reconnect);
            _stateUpdater.UpdateState();
            return con;
        }

        class HandleStreams : IStreamHandler
        {
            private readonly UserStreamsReceiver _parent;

            public HandleStreams(UserStreamsReceiver parent)
            {
                Debug.WriteLine("*USERSTREAMS* " + parent._account.UnreliableScreenName + ": Successufully subscribed.");
                this._parent = parent;
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
                BackstageModel.RegisterEvent(new UserStreamsDisconnectedEvent(this._parent.Account, streamDisconnect.Reason));
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
                        NotificationService.NotifyFavorited(item.Source, item.Status);
                        break;
                    case StreamStatusActivityEvent.Unfavorite:
                        NotificationService.NotifyUnfavorited(item.Source, item.Status);
                        break;
                }
            }

            public void OnTrackLimit(StreamTrackLimit item)
            {
                NotificationService.NotifyLimitationInfoGot(this._parent.Account, (int)item.UndeliveredCount);
            }

            public async void OnUserActivity(StreamUserActivity item)
            {
                var active = item.Source.Id == this._parent.Account.Id;
                var passive = item.Target.Id == this._parent.Account.Id;
                var reldata = this._parent.Account.RelationData;
                switch (item.Event)
                {
                    case StreamUserActivityEvent.Unknown:
                        BackstageModel.RegisterEvent(new UnknownEvent(item.Source, item.EventRawString));
                        break;
                    case StreamUserActivityEvent.Follow:
                        if (active)
                        {
                            await reldata.SetFollowingAsync(item.Target.Id, true);
                        }
                        if (passive)
                        {
                            await reldata.SetFollowerAsync(item.Source.Id, true);
                        }
                        NotificationService.NotifyFollowed(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Unfollow:
                        if (active)
                        {
                            await reldata.SetFollowingAsync(item.Target.Id, false);
                        }
                        if (passive)
                        {
                            await reldata.SetFollowerAsync(item.Source.Id, false);
                        }
                        NotificationService.NotifyUnfollowed(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Block:
                        if (active)
                        {
                            await reldata.SetFollowingAsync(item.Target.Id, false);
                            await reldata.SetFollowerAsync(item.Target.Id, false);
                            await reldata.SetBlockingAsync(item.Target.Id, true);
                        }
                        if (passive)
                        {
                            await reldata.SetFollowerAsync(item.Target.Id, false);
                        }
                        NotificationService.NotifyBlocked(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.Unblock:
                        if (active)
                        {
                            await reldata.SetBlockingAsync(item.Target.Id, false);
                        }
                        NotificationService.NotifyUnblocked(item.Source, item.Target);
                        break;
                    case StreamUserActivityEvent.UserUpdate:
                        NotificationService.NotifyUserUpdated(item.Source);
                        break;
                }
            }
        }

        #region Error handlers

        private BackOffMode _currentBackOffMode = BackOffMode.None;
        private long _currentBackOffWaitCount;
        private int _hardErrorRetryCount;

        private void ResetErrorParams()
        {
            this._currentBackOffMode = BackOffMode.None;
            this._currentBackOffWaitCount = 0;
            this._hardErrorRetryCount = 0;
        }

        private void HandleException(Exception ex)
        {
            Debug.WriteLine("*USERSTREAMS* catch exception: " + ex.Message);
            Debug.WriteLine(ex.ToString());
            this.CleanupConnection();
            var tae = ex as TwitterApiException;
            if (tae != null)
            {
                _stateUpdater.UpdateState(_account.UnreliableScreenName + ": User Streamsが切断されました(コード: " + (int)tae.StatusCode + ")");
                switch (tae.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        // ERR: Unauthorized, invalid OAuth request?
                        if (this.CheckHardError())
                        {
                            this.RaiseDisconnectedByError(
                                "ユーザー認証が行えません。",
                                "PCの時刻設定が正しいか確認してください。回復しない場合は、OAuth認証を再度行ってください。");
                            return;
                        }
                        break;
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.NotFound:
                        if (this.CheckHardError())
                        {
                            this.RaiseDisconnectedByError(
                                "ユーザーストリーム接続が一時的、または恒久的に利用できなくなっています。",
                                "エンドポイントへの接続時にアクセスが拒否されたか、またはエンドポイントが削除されています。");
                            return;
                        }
                        break;
                    case HttpStatusCode.NotAcceptable:
                    case HttpStatusCode.RequestEntityTooLarge:
                        this.RaiseDisconnectedByError(
                            "トラックしているキーワードが長すぎるか、不正な可能性があります。",
                            "(トラック中のキーワード:" + this._trackKeywords.JoinString(", ") + ")");
                        return;
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                        this.RaiseDisconnectedByError(
                            "ユーザーストリームに接続できません。",
                            "(システム エラー: 416 Range Unacceptable. Elevated permission is required or paramter is out of range.)");
                        return;
                    case (HttpStatusCode)420:
                        // ERR: Too many connections
                        // (other client is already connected?)
                        this.RaiseDisconnectedByError(
                            "ユーザーストリーム接続が制限されています。",
                            "Krileが多重起動していないか確認してください。短時間に何度も接続を試みていた場合は、しばらく待つと再接続できるようになります。");
                        return;
                }
                // else -> backoff
                if (this._currentBackOffMode == BackOffMode.ProtocolError)
                {
                    // wait count is raised exponentially.
                    this._currentBackOffWaitCount *= 2;
                }
                else
                {
                    this._currentBackOffWaitCount = 5000;
                    this._currentBackOffMode = BackOffMode.ProtocolError;
                }
                // max wait is 320 sec.
                if (this._currentBackOffWaitCount >= 320000)
                {
                    this.RaiseDisconnectedByError(
                        "Twitterが不安定な状態になっています。",
                        "プロトコル エラーにより、ユーザーストリームに既定のリトライ回数内で接続できませんでした。");
                    _stateUpdater.UpdateState(_account.UnreliableScreenName + ": User Streamsへ接続できませんでした(プロトコル エラー)");
                    return;
                }
            }
            else
            {
                // network error
                // -> backoff
                if (this._currentBackOffMode == BackOffMode.NetworkError)
                {
                    // wait count is raised linearly.
                    this._currentBackOffMode += 250;
                }
                else
                {
                    // wait starts 250ms
                    this._currentBackOffWaitCount = 250;
                    this._currentBackOffMode = BackOffMode.NetworkError;
                }
                // max wait is 16 sec.
                if (this._currentBackOffWaitCount >= 16000)
                {
                    this.RaiseDisconnectedByError(
                        "Twitterが不安定な状態になっています。",
                        "ネットワーク エラーにより、ユーザーストリームに規定のリトライ回数内で接続できませんでした。");
                    _stateUpdater.UpdateState(_account.UnreliableScreenName + ": User Streamsへ接続できませんでした(ネットワーク エラー)");
                    return;
                }
            }
            Debug.WriteLine("*** USER STREAMS error ***" + Environment.NewLine + ex);
            Debug.WriteLine(" -> reconnect.");
            // parsing error, auto-reconnect
            _stateUpdater.UpdateState(_account.UnreliableScreenName + ": User Streamsへの再接続を試みています...(" + _currentBackOffWaitCount + " msec 待機しています)");
            this._currentConnection.Add(
                Observable.Timer(TimeSpan.FromMilliseconds(this._currentBackOffWaitCount))
                          .Subscribe(_ => this.Reconnect()));
        }

        private bool CheckHardError()
        {
            this._hardErrorRetryCount++;
            if (this._hardErrorRetryCount > HardErrorRetryMaxCount)
            {
                return true;
            }
            return false;
        }

        private void RaiseDisconnectedByError(string header, string detail)
        {
            this.CleanupConnection();
            Debug.WriteLine("*** USER STREAM DISCONNECT ***" + Environment.NewLine + header + Environment.NewLine + detail + Environment.NewLine);
            var discone = new UserStreamsDisconnectedEvent(this.Account, header + " - " + detail);
            BackstageModel.RegisterEvent(discone);
            this.ConnectionState = UserStreamsConnectionState.WaitForReconnection;
            this._currentConnection.Add(
                Observable.Timer(TimeSpan.FromMinutes(5))
                          .Do(_ => BackstageModel.RemoveEvent(discone))
                          .Subscribe(_ => this.Reconnect()));
        }

        #endregion

        private void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("UserStreamsReceiver");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~UserStreamsReceiver()
        {
            this.Dispose(false);
        }

        private bool _disposed;
        private void Dispose(bool disposing)
        {
            this._disposed = true;
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
