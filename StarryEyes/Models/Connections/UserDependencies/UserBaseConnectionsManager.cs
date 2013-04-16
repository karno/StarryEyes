using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Connections.UserDependencies
{
    /// <summary>
    ///     Provides management for connect to twitter.
    /// </summary>
    public static class UserBaseConnectionsManager
    {
        private static readonly object ConnectionGroupsLocker = new object();

        private static readonly SortedDictionary<long, ConnectionGroup> ConnectionGroups
            = new SortedDictionary<long, ConnectionGroup>();

        private static readonly object TrackingLocker = new object();

        private static readonly SortedDictionary<string, UserStreamsConnection> TrackResolver
            = new SortedDictionary<string, UserStreamsConnection>();

        private static readonly SortedDictionary<string, int> TrackReferenceCount
            = new SortedDictionary<string, int>();

        // tracked keywords relied on stopped/removed streamings.
        private static List<string> _danglingKeywords = new List<string>();

        public static IEnumerable<string> DanglingKeywords
        {
            get { return _danglingKeywords.AsEnumerable(); }
        }

        /// <summary>
        ///     Update connection states.
        ///     <para />
        ///     Apply current setting params, maintain the connections.
        /// </summary>
        /// <param name="enforceReconnection">all user streams connection will be reconnected with enforced</param>
        public static void Update(bool enforceReconnection = false)
        {
            // create look-up dictionary.
            var settings = AccountsStore.Accounts.ToDictionary(k => k.UserId);

            lock (ConnectionGroupsLocker)
            {
                // determine removed users ids and finalize for each.
                ConnectionGroups.Values
                                .Select(g => g.UserId)
                                .Where(g => !settings.ContainsKey(g))
                                .Select(s => ConnectionGroups[s])
                                .ToArray() // freeze once
                                .Do(c => ConnectionGroups.Remove(c.UserId))
                                .Do(c =>
                                {
                                    var con = c.UserStreamsConnection;
                                    if (con != null && con.TrackKeywords != null)
                                    {
                                        con.TrackKeywords.ForEach(s => _danglingKeywords.Add(s));
                                    }
                                })
                                .ForEach(c => c.Dispose());

                // add new users
                settings.Select(s => s.Key)
                        .Except(ConnectionGroups.Keys)
                        .Select(i => new ConnectionGroup(i))
                        .ForEach(c => ConnectionGroups.Add(c.UserId, c));

                // stop cancelled streamings
                ConnectionGroups.Values
                                .Where(c => c.IsUserStreamsEnabled &&
                                            !settings[c.UserId].IsUserStreamsEnabled)
                                .Do(c =>
                                {
                                    var con = c.UserStreamsConnection;
                                    if (con != null && con.TrackKeywords != null)
                                    {
                                        con.TrackKeywords.ForEach(s => _danglingKeywords.Add(s));
                                    }
                                })
                                .ForEach(c => c.StopStreaming());

                // start new streamings
                ConnectionGroups.Values
                                .Where(c => !c.IsUserStreamsEnabled && settings[c.UserId].IsUserStreamsEnabled)
                                .ForEach(c =>
                                {
                                    // take danglings
                                    var assign = _danglingKeywords
                                        .Take(UserStreamsConnection.MaxTrackingKeywordCounts);
                                    c.StartStreaming(assign);
                                    // update dangling list.
                                    _danglingKeywords = _danglingKeywords
                                        .Skip(UserStreamsConnection.MaxTrackingKeywordCounts)
                                        .ToList();
                                });

                if (_danglingKeywords.Count > 0 || enforceReconnection)
                {
                    // if dangling keywords existed, assign them.
                    ConnectionGroups.Values
                                    .Select(c => c.UserStreamsConnection)
                                    .Where(u => u != null)
                                    .Where(
                                        u =>
                                        u.TrackKeywords.Count() < UserStreamsConnection.MaxTrackingKeywordCounts ||
                                        enforceReconnection)
                                    .OrderBy(u => u.TrackKeywords.Count())
                                    .TakeWhile(_ => _danglingKeywords.Count > 0 || enforceReconnection)
                                    .ForEach(f =>
                                    {
                                        var assignable = UserStreamsConnection.MaxTrackingKeywordCounts -
                                                         f.TrackKeywords.Count();
                                        f.TrackKeywords = f.TrackKeywords.Concat(_danglingKeywords.Take(assignable));
                                        // reconnect this
                                        f.Connect();
                                        _danglingKeywords = _danglingKeywords.Skip(assignable).ToList();
                                    });
                }
            }
            // dangling keywords should be resolved as null.
            _danglingKeywords.ForEach(s => TrackResolver[s] = null);
            NotifyDanglings();
        }

        /// <summary>
        ///     Notify dangling state.
        /// </summary>
        private static void NotifyDanglings()
        {
            if (_danglingKeywords.Count > 0)
            {
                BackpanelModel.RegisterEvent(new StreamingKeywordDanglingEvent());
            }
            else
            {
                BackpanelModel.RemoveEvent(new StreamingKeywordDanglingEvent().Id);
            }
        }

        /// <summary>
        ///     Add tracking keywords.
        /// </summary>
        /// <param name="keyword">adding keyword</param>
        public static void AddTrackKeyword(string keyword)
        {
            lock (TrackingLocker)
            {
                if (TrackReferenceCount.ContainsKey(keyword))
                {
                    // already registered.
                    TrackReferenceCount[keyword]++;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("*** TRACK ADD:" + keyword);
                    TrackReferenceCount[keyword] = 1;
                    // connect
                    var connection = GetMostSuitableConnection();
                    if (connection == null)
                    {
                        _danglingKeywords.Add(keyword);
                        TrackResolver.Add(keyword, null);
                    }
                    else
                    {
                        TrackResolver.Add(keyword, connection);
                        connection.TrackKeywords =
                            connection.TrackKeywords.Append(new[] { keyword });
                        connection.Connect();
                    }
                }
                NotifyDanglings();
            }
        }

        private static UserStreamsConnection GetMostSuitableConnection()
        {
            lock (ConnectionGroupsLocker)
            {
                return ConnectionGroups.Values
                                       .Select(c => c.UserStreamsConnection)
                                       .Where(u => u != null)
                                       .Where(
                                           u => u.TrackKeywords.Count() < UserStreamsConnection.MaxTrackingKeywordCounts)
                                       .OrderBy(u => u.TrackKeywords.Count())
                                       .FirstOrDefault();
            }
        }

        /// <summary>
        ///     Remove tracking keywords.
        /// </summary>
        /// <param name="keyword">removing keyword</param>
        public static void RemoveTrackKeyword(string keyword)
        {
            lock (TrackingLocker)
            {
                if (!TrackReferenceCount.ContainsKey(keyword))
                    throw new ArgumentException("Keyword is not registered.");
                if (!TrackResolver.ContainsKey(keyword))
                    throw new ArgumentException("Keyword is registered but receiver has not assigned.");
                TrackReferenceCount[keyword]--;
                if (TrackReferenceCount[keyword] == 0)
                {
                    TrackReferenceCount.Remove(keyword);
                    var tracked = TrackResolver[keyword];
                    if (tracked != null) // if track-resolver bind key to null value, that's dangling word.
                    {
                        TrackResolver.Remove(keyword);
                        System.Diagnostics.Debug.WriteLine("*** TRACK REMOVE: " + keyword);
                        tracked.TrackKeywords = tracked.TrackKeywords.Except(new[] { keyword });
                        tracked.Connect();
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Groups of connection.
    /// </summary>
    internal sealed class ConnectionGroup : IDisposable
    {
        private readonly long _userId;
        private bool _isDisposed;
        private readonly UserInfoReceiver _userInfoReceiver;
        private readonly UserTimelinesReceiver _userTimelineReceiver;
        private readonly UserRelationReceiver _userRelationReceiver;
        private UserStreamsConnection _userStreams;

        public ConnectionGroup(long id)
        {
            _userId = id;
            _userTimelineReceiver = new UserTimelinesReceiver(AuthInfo) { IsActivated = true };
            _userInfoReceiver = new UserInfoReceiver(AuthInfo) { IsActivated = true };
            _userRelationReceiver = new UserRelationReceiver(AuthInfo) { IsActivated = true };
        }

        public long UserId
        {
            get { return _userId; }
        }

        private AuthenticateInfo AuthInfo
        {
            get
            {
                return AccountsStore.Accounts
                                    .Select(a => a.AuthenticateInfo)
                                    .FirstOrDefault(i => i.Id == UserId);
            }
        }

        public UserTimelinesReceiver UserDependentTimelinesReceiver
        {
            get { return _userTimelineReceiver; }
        }

        public UserInfoReceiver UserInfoReceiver
        {
            get { return _userInfoReceiver; }
        }

        public UserRelationReceiver UserRelationReceiver
        {
            get { return _userRelationReceiver; }
        }

        public UserStreamsConnection UserStreamsConnection
        {
            get { return _userStreams; }
        }

        /// <summary>
        ///     Get/Set User Streams state.
        /// </summary>
        public bool IsUserStreamsEnabled
        {
            get
            {
                CheckDispose();
                return _userStreams != null;
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StopStreaming()
        {
            this.CheckDispose();
            var stream = Interlocked.Exchange(ref _userStreams, null);
            if (stream == null) return;
            stream.Dispose();
        }

        public bool StartStreaming(IEnumerable<string> trackings = null)
        {
            CheckDispose();
            var newcon = new UserStreamsConnection(AuthInfo);
            var connection = Interlocked.CompareExchange(ref _userStreams, newcon, null);
            if (connection != null)
            {
                // already established
                newcon.Dispose();
                return false;
            }
            newcon.OnAccidentallyDisconnected += OnAccidentallyDisconnected;
            newcon.TrackKeywords = trackings ?? Enumerable.Empty<string>();
            newcon.Connect();
            return true;
        }

        private void ReconnectStreaming()
        {
            if (_isDisposed) return;
            var newcon = new UserStreamsConnection(AuthInfo);
            var connection = Interlocked.CompareExchange(ref _userStreams, newcon, null);
            if (connection == null)
            {
                // streaming cancelled
                return;
            }
            newcon.OnAccidentallyDisconnected += OnAccidentallyDisconnected;
            newcon.TrackKeywords = connection.TrackKeywords ?? Enumerable.Empty<string>();
            newcon.Connect();
        }

        private void OnAccidentallyDisconnected()
        {
            Observable.Timer(TimeSpan.FromMinutes(5))
                      .Subscribe(_ => this.ReconnectStreaming());
        }

        private void CheckDispose()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("ConnectionGroup");
        }

        ~ConnectionGroup()
        {
            if (!_isDisposed)
            {
                Dispose(false);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (this._userStreams != null)
            {
                var disposal = this._userStreams;
                this._userStreams = null;
                disposal.Dispose();
            }
            this._userTimelineReceiver.Dispose();
        }
    }
}