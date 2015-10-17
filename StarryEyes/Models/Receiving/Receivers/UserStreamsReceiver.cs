using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels.Streams;
using StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Events;
using StarryEyes.Anomaly.TwitterApi.DataModels.Streams.Internals;
using StarryEyes.Anomaly.TwitterApi.Streams;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.SystemEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Receiving.Receivers
{
    public sealed class UserStreamsReceiver : IDisposable
    {
        public const int MaxTrackingKeywordCounts = 100;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    Reconnect();
                }
            }
        }

        private readonly TimeSpan _userStreamTimeout = TimeSpan.FromSeconds(70);
        private readonly CancellationTokenSource _receiverTokenSource;
        private CancellationTokenSource _cancellationTokenSource;

        private bool _disposed;

        private readonly TwitterAccount _account;
        private readonly IStreamHandler _handler;

        #region Connection State Management / Notification

        private UserStreamsConnectionState _state = UserStreamsConnectionState.Disconnected;
        public event Action StateChanged;
        private readonly StateUpdater _stateUpdater;

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
                StateChanged.SafeInvoke();
            }
        }

        #endregion

        #region error handling/backoff constants

        private const int MaxHardErrorCount = 3;

        private const int ProtocolErrorInitialWait = 5000; // 5 sec

        private const int ProtocolErrorMaxWait = 320000; // 320 sec

        private const int NetworkErrorInitialWait = 250; // 0.25 sec

        private const int NetworkErrorMaxWait = 16000; // 16 sec

        #endregion

        private BackoffMode _backoffMode = BackoffMode.None;

        private long _backoffWait = 0;

        private int _hardErrorCount = 0;

        #region User Stream properties

        private string[] _trackKeywords;
        /// <summary>
        /// Track keywords
        /// </summary>
        public IEnumerable<string> TrackKeywords
        {
            get { return _trackKeywords ?? Enumerable.Empty<string>(); }
            set
            {
                // automatic reconnect
                var prev = _trackKeywords;
                _trackKeywords = (value ?? Enumerable.Empty<string>()).ToArray();
                if (prev == null ||
                    prev.Length != _trackKeywords.Length ||
                    prev.Any(s => _trackKeywords.Contains(s)))
                {
                    Task.Run(() => Reconnect());
                }
            }
        }

        #endregion

        public UserStreamsReceiver(TwitterAccount account)
        {
            _stateUpdater = new StateUpdater();
            _handler = InitializeHandler();
            _receiverTokenSource = new CancellationTokenSource();
            _account = account;
            _cancellationTokenSource = null;
            ConnectionState = UserStreamsConnectionState.Disconnected;
        }

        private IStreamHandler InitializeHandler()
        {
            var handler = StreamHandler.Create(StatusInbox.Enqueue,
                ex =>
                {
                    BehaviorLogger.Log("U/S", _account.UnreliableScreenName + ":" + ex.ToString());
                    BackstageModel.RegisterEvent(new StreamDecodeFailedEvent(_account.UnreliableScreenName, ex));
                });
            handler.AddHandler<StreamDelete>(d => StatusInbox.EnqueueRemoval(d.Id));
            handler.AddHandler<StreamDisconnect>(
                d => BackstageModel.RegisterEvent(new UserStreamsDisconnectedEvent(_account, d.Reason)));
            handler.AddHandler<StreamLimit>(
                limit => NotificationService.NotifyLimitationInfoGot(_account, (int)limit.UndeliveredCount));
            handler.AddHandler<StreamStatusEvent>(s =>
            {
                switch (s.Event)
                {
                    case StatusEvents.Unknown:
                        BackstageModel.RegisterEvent(new UnknownEvent(s.Source, s.RawEvent));
                        break;
                    case StatusEvents.Favorite:
                        NotificationService.NotifyFavorited(s.Source, s.TargetObject);
                        break;
                    case StatusEvents.Unfavorite:
                        NotificationService.NotifyUnfavorited(s.Source, s.TargetObject);
                        break;
                    case StatusEvents.FavoriteRetweet:
                        NotificationService.NotifyFavorited(s.Source, s.TargetObject);
                        break;
                    case StatusEvents.RetweetRetweet:
                        NotificationService.NotifyRetweeted(s.Source, s.TargetObject.RetweetedOriginal, s.TargetObject);
                        break;
                    case StatusEvents.Quote:
                        // do nothing
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (s.TargetObject != null)
                {
                    StatusInbox.Enqueue(s.TargetObject);
                }
            });
            handler.AddHandler<StreamUserEvent>(async item =>
            {
                var active = item.Source.Id == _account.Id;
                var passive = item.Target.Id == _account.Id;
                var reldata = _account.RelationData;
                switch (item.Event)
                {
                    case UserEvents.Unknown:
                        BackstageModel.RegisterEvent(new UnknownEvent(item.Source, item.RawEvent));
                        break;
                    case UserEvents.Follow:
                        if (active)
                        {
                            await reldata.Followings.SetAsync(item.Target.Id, true).ConfigureAwait(false);
                        }
                        if (passive)
                        {
                            await reldata.Followers.SetAsync(item.Source.Id, true).ConfigureAwait(false);
                        }
                        NotificationService.NotifyFollowed(item.Source, item.Target);
                        break;
                    case UserEvents.Unfollow:
                        if (active)
                        {
                            await reldata.Followings.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                        }
                        if (passive)
                        {
                            await reldata.Followers.SetAsync(item.Source.Id, false).ConfigureAwait(false);
                        }
                        NotificationService.NotifyUnfollowed(item.Source, item.Target);
                        break;
                    case UserEvents.Block:
                        if (active)
                        {
                            await reldata.Followings.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                            await reldata.Followers.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                            await reldata.Blockings.SetAsync(item.Target.Id, true).ConfigureAwait(false);
                        }
                        if (passive)
                        {
                            await reldata.Followers.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                        }
                        NotificationService.NotifyBlocked(item.Source, item.Target);
                        break;
                    case UserEvents.Unblock:
                        if (active)
                        {
                            await reldata.Blockings.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                        }
                        NotificationService.NotifyUnblocked(item.Source, item.Target);
                        break;
                    case UserEvents.UserUpdate:
                        NotificationService.NotifyUserUpdated(item.Source);
                        break;
                    case UserEvents.Mute:
                        if (active)
                        {
                            await reldata.Mutes.SetAsync(item.Target.Id, true).ConfigureAwait(false);
                        }
                        NotificationService.NotifyBlocked(item.Source, item.Target);
                        break;
                    case UserEvents.UnMute:
                        if (active)
                        {
                            await reldata.Mutes.SetAsync(item.Target.Id, false).ConfigureAwait(false);
                        }
                        NotificationService.NotifyUnblocked(item.Source, item.Target);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            return handler;
        }

        public void Reconnect()
        {
            if (_disposed)
            {
                return;
            }
            _stateUpdater.UpdateState("@" + _account.UnreliableScreenName + ": " +
                                           ReceivingResources.UserStreamReconnecting);
            Connect(_receiverTokenSource.Token);
        }

        /// <summary>
        /// Begin receiving streams
        /// </summary>
        /// <param name="token">cancellation token</param>
        /// <returns>DateTime.MaxValue</returns>
        private void Connect(CancellationToken token)
        {
            // create new token and swap for old one.
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var oldcts = Interlocked.Exchange(ref _cancellationTokenSource, cts);

            // cancel previous connection
            try
            {
                oldcts?.Cancel();
            }
            catch
            {
                // ignored
            }
            finally
            {
                oldcts?.Dispose();
            }

            if (!_isEnabled || token.IsCancellationRequested)
            {
                _stateUpdater.UpdateState();
                ConnectionState = UserStreamsConnectionState.Disconnected;
                return;
            }

            _stateUpdater.UpdateState("@" + _account.UnreliableScreenName + ": " +
                                           ReceivingResources.UserStreamReconnecting);

            // call ExecuteInternalAsync asynchronously with created token
            Task.Run(async () =>
            {
                try
                {
                    await ConnectInternalAsync(cts.Token).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }, cts.Token);
        }

        private async Task ConnectInternalAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConnectionState = UserStreamsConnectionState.Connecting;
                try
                {
                    await UserStreams.Connect(_account, ParseLine, _userStreamTimeout, cancellationToken, TrackKeywords,
                        _account.ReceiveRepliesAll, _account.ReceiveFollowingsActivity).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ConnectionState = UserStreamsConnectionState.WaitForReconnection;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    if (await HandleException(ex).ConfigureAwait(false))
                    {
                        // if handled: continue running
                        continue;
                    }
                    ConnectionState = UserStreamsConnectionState.Disconnected;
                    throw;
                }
            }
            ConnectionState = UserStreamsConnectionState.Disconnected;
        }

        private void ParseLine(string json)
        {
            // reset counts
            _hardErrorCount = 0;
            _stateUpdater.UpdateState();
            ConnectionState = UserStreamsConnectionState.Connected;
            UserStreamParser.ParseStreamLine(json, _handler);
        }

        private async Task<bool> HandleException(Exception ex)
        {
            Log("Exception on User Stream Receiver: " + Environment.NewLine + ex);
            var tx = ex as TwitterApiException;
            if (tx != null)
            {
                // protocol error
                Log($"Twitter API Exception: [status-code: {tx.StatusCode} twitter-code: {tx.TwitterErrorCode}]");
                _stateUpdater.UpdateState(_account.UnreliableScreenName +
                                          ReceivingResources.UserStreamDisconnectedFormat.SafeFormat(
                                              (int)tx.StatusCode, tx.TwitterErrorCode));
                _handler.OnMessage(new StreamErrorMessage(_account, tx.StatusCode, tx.TwitterErrorCode));
                switch (tx.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        Log("Authorization failed.");
                        if (_hardErrorCount > MaxHardErrorCount)
                        {
                            return false;
                        }
                        break;
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.NotFound:
                        Log("Endpoint not found / not accessible.");
                        if (_hardErrorCount > MaxHardErrorCount)
                        {
                            return false;
                        }
                        break;
                    case HttpStatusCode.NotAcceptable:
                    case HttpStatusCode.RequestEntityTooLarge:
                        Log("Specified argument could not be accepted.");
                        return false;
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                        Log("Permission denied / Parameter out of range");
                        return false;
                    case (HttpStatusCode)420: // Too many connections
                        Log("Too many connections are established.");
                        return false;
                }
                // general protocol error
                if (_backoffMode == BackoffMode.ProtocolError)
                {
                    // exponential backoff
                    _backoffWait *= 2;
                }
                else
                {
                    _backoffWait = ProtocolErrorInitialWait;
                    _backoffMode = BackoffMode.ProtocolError;
                }
                if (_backoffWait >= ProtocolErrorMaxWait)
                {
                    Log("Protocol backoff limit exceeded.");
                    _stateUpdater.UpdateState(_account.UnreliableScreenName + ": " +
                                              ReceivingResources.ConnectFailedByProtocol);
                    return false;
                }
            }
            else
            {
                // network error
                if (_backoffMode == BackoffMode.NetworkError)
                {
                    // linear backoff
                    _backoffWait += NetworkErrorInitialWait;
                }
                else
                {
                    _backoffWait = NetworkErrorInitialWait;
                    _backoffMode = BackoffMode.NetworkError;
                }
                if (_backoffWait >= NetworkErrorMaxWait)
                {
                    Log("Network backoff limit exceeded.");
                    _stateUpdater.UpdateState(_account.UnreliableScreenName + ": " +
                                              ReceivingResources.ConnectFailedByNetwork);
                    return false;
                }
            }
            Log($"Waiting reconnection... [{_backoffWait} ms]");
            _stateUpdater.UpdateState(_account.UnreliableScreenName + ": " +
                                      ReceivingResources.ReconnectingFormat.SafeFormat(_backoffWait));
            _handler.OnMessage(new StreamWaitMessage(_account, _backoffWait));
            await Task.Delay(TimeSpan.FromMilliseconds(_backoffWait)).ConfigureAwait(false);
            return true;
        }

        private void Log(string body)
        {
            var splitEach = body.Split('\r', '\n').Select(t => t.Trim()).Where(t => !String.IsNullOrEmpty(t));
            foreach (var text in splitEach)
            {
                Debug.WriteLine("[USER-STREAMS] " + text);
                _handler.Log(text);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UserStreamsReceiver()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            try
            {
                _cancellationTokenSource?.Cancel();
                _receiverTokenSource.Cancel();
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _receiverTokenSource.Dispose();
            }
        }
    }

    internal enum BackoffMode
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
