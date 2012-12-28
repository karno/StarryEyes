using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

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

        public static bool ReconnectImmediate { get; set; }

        /// <summary>
        ///     Update connection states.
        ///     <para />
        ///     Apply current setting params, maintain the connections.
        /// </summary>
        /// <param name="enforceReconnection">all user streams connection will be reconnected with enforced</param>
        public static void Update(bool enforceReconnection = false)
        {
            // create look-up dictionary.
            ILookup<long, AccountSetting> settings = AccountsStore.Accounts.ToLookup(k => k.UserId);
            lock (ConnectionGroupsLocker)
            {
                // determine removed users ids and finalize for each.
                ConnectionGroups.Values
                                .Select(g => g.UserId)
                                .Where(g => !settings.Contains(g))
                                .Select(s => ConnectionGroups[s])
                                .ToArray() // freeze once
                                .Do(c => ConnectionGroups.Remove(c.UserId))
                                .Do(c =>
                                {
                                    UserStreamsConnection con = c.UserStreamsConnection;
                                    if (con != null && con.TrackKeywords != null)
                                    {
                                        con.TrackKeywords.ForEach(s => _danglingKeywords.Add(s));
                                    }
                                })
                                .ForEach(c => c.Dispose());

                // determine cancelled streamings
                ConnectionGroups.Values
                                .Where(c => c.IsUserStreamsEnabled &&
                                            !settings[c.UserId].First().IsUserStreamsEnabled)
                                .Do(c =>
                                {
                                    UserStreamsConnection con = c.UserStreamsConnection;
                                    if (con != null && con.TrackKeywords != null)
                                    {
                                        con.TrackKeywords.ForEach(s => _danglingKeywords.Add(s));
                                    }
                                })
                                .ForEach(c => c.IsUserStreamsEnabled = false);

                // connects new.
                // dangling keywords are mostly assigned for it.
                settings.Select(s => s.Key)
                        .Except(ConnectionGroups.Keys)
                        .Select(i => new ConnectionGroup(i))
                        .Do(g => ConnectionGroups.Add(g.UserId, g))
                        .Where(g => settings[g.UserId].First().IsUserStreamsEnabled)
                        .ForEach(f =>
                        {
                            // take danglings
                            IEnumerable<string> assign = _danglingKeywords
                                .Take(UserStreamsConnection.MaxTrackingKeywordCounts);
                            f.UserStreamsStartsWith(assign);
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
                                        int assignable = UserStreamsConnection.MaxTrackingKeywordCounts -
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
                AppInformationHub.PublishInformation(
                    new AppInformation(AppInformationKind.Warning,
                                       "ConnectionManager_UserStreamsTrackDanglings",
                                       "受信されていないトラッキング キーワードがあります。",
                                       "トラッキング キーワードに対し、ユーザーストリーム接続数が不足しています。"));
            }
        }

        /// <summary>
        ///     Add tracking keywords.
        /// </summary>
        /// <param name="keyword">adding keyword</param>
        /// <param name="reconnectImmediate">reconnect user streams immediately if required.</param>
        public static void AddTrackKeyword(string keyword, bool? reconnectImmediate = null)
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
                    TrackReferenceCount[keyword] = 1;
                    // connect
                    UserStreamsConnection connection = GetMostSuitableConnection();
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
                        if (reconnectImmediate ?? ReconnectImmediate)
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
        /// <param name="reconnectImmediate">reconnect user streams immediately if required.</param>
        public static void RemoveTrackKeyword(string keyword, bool? reconnectImmediate = null)
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
                    UserStreamsConnection tracked = TrackResolver[keyword];
                    if (tracked != null) // if track-resolver bind key to null value, that's dangling word.
                    {
                        TrackResolver.Remove(keyword);
                        tracked.TrackKeywords = tracked.TrackKeywords.Except(new[] { keyword });
                        if (reconnectImmediate ?? ReconnectImmediate)
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
        private readonly UserInfoReceiver _userInfoReceiver;
        private readonly UserTimelinesReceiver _userTimelineReceiver;
        private bool _isDisposed;
        private UserStreamsConnection _userStreams;

        public ConnectionGroup(long id)
        {
            _userId = id;
            _userTimelineReceiver = new UserTimelinesReceiver(AuthInfo);
            _userTimelineReceiver.IsActivated = true;
            _userInfoReceiver = new UserInfoReceiver(AuthInfo);
            _userInfoReceiver.IsActivated = true;
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
            set
            {
                CheckDispose();
                if (value == IsUserStreamsEnabled) return;
                if (value && AuthInfo != null)
                {
                    // connect
                    _userStreams = new UserStreamsConnection(AuthInfo);
                    _userStreams.Connect();
                }
                else if (_userStreams != null)
                {
                    // disconnect
                    _userStreams.Dispose();
                    _userStreams = null;
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void UserStreamsStartsWith(IEnumerable<string> trackKeywords)
        {
            CheckDispose();
            if (IsUserStreamsEnabled) // already connected
                return;
            _userStreams = new UserStreamsConnection(AuthInfo);
            _userStreams.IsConnectionAliveEvent += UserStreamsStateChanged;
            _userStreams.TrackKeywords = trackKeywords;
            _userStreams.Connect();
        }

        private void UserStreamsStateChanged(bool state)
        {
            // TODO: Implementation
        }

        private void CheckDispose()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("ConnectionGroup");
        }

        ~ConnectionGroup()
        {
            if (!_isDisposed)
                Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_userStreams != null)
            {
                UserStreamsConnection disposal = _userStreams;
                _userStreams = null;
                disposal.Dispose();
            }
            _userTimelineReceiver.Dispose();
        }
    }
}