using System;
using System.Collections.Generic;
using System.Linq;
using StarryEyes.Mystique.Models.Connection.Polling;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.Mystique.Settings;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.UserDependency
{
    /// <summary>
    /// Provides management for connect to twitter.
    /// </summary>
    public static class UserDependencyConnectionsManager
    {
        private static object connectionGroupsLocker = new object();
        private static SortedDictionary<long, ConnectionGroup> connectionGroups =
            new SortedDictionary<long, ConnectionGroup>();

        private static object trackingLocker = new object();
        private static SortedDictionary<string, UserStreamsConnection> trackResolver =
            new SortedDictionary<string, UserStreamsConnection>();
        private static SortedDictionary<string, int> trackReferenceCount =
            new SortedDictionary<string, int>();

        // tracked keywords relied on stopped/removed streamings.
        private static List<string> danglingKeywords = new List<string>();
        public static IEnumerable<string> DanglingKeywords
        {
            get { return danglingKeywords.AsEnumerable(); }
        }

        /// <summary>
        /// Update connection states.<para />
        /// Apply current setting params, maintain the connections.
        /// </summary>
        /// <param name="enforceReconnection">all user streams connection will be reconnected with enforced</param>
        public static void Update(bool enforceReconnection = false)
        {
            // create look-up dictionary.
            var settings = Setting.Accounts.Value.ToLookup(k => k.UserId);
            lock (connectionGroupsLocker)
            {
                // determine removed users ids and finalize for each.
                connectionGroups.Values
                    .Select(g => g.UserId)
                    .Where(g => !settings.Contains(g))
                    .Select(s => connectionGroups[s])
                    .ToArray() // freeze once
                    .Do(c => connectionGroups.Remove(c.UserId))
                    .Do(c =>
                    {
                        var con = c.UserStreamsConnection;
                        if (con != null && con.TrackKeywords != null)
                        {
                            con.TrackKeywords.ForEach(s => danglingKeywords.Add(s));
                        }
                    })
                    .ForEach(c => c.Dispose());

                // determine cancelled streamings
                connectionGroups.Values
                    .Where(c => c.IsUserStreamsEnabled &&
                        !settings[c.UserId].First().IsUserStreamsEnabled)
                    .Do(c =>
                    {
                        var con = c.UserStreamsConnection;
                        if (con != null && con.TrackKeywords != null)
                        {
                            con.TrackKeywords.ForEach(s => danglingKeywords.Add(s));
                        }
                    })
                    .ForEach(c => c.IsUserStreamsEnabled = false);

                // connects new.
                // dangling keywords are mostly assigned for it.
                settings.Select(s => s.Key)
                    .Except(connectionGroups.Keys)
                    .Select(i => new ConnectionGroup(i))
                    .Do(g => connectionGroups.Add(g.UserId, g))
                    .Where(g => settings[g.UserId].First().IsUserStreamsEnabled)
                    .ForEach(f =>
                    {
                        // take danglings
                        var assign = danglingKeywords
                            .Take(UserStreamsConnection.MaxTrackingKeywordCounts);
                        f.UserStreamsStartsWith(assign);
                        // update dangling list.
                        danglingKeywords = danglingKeywords
                            .Skip(UserStreamsConnection.MaxTrackingKeywordCounts)
                            .ToList();
                    });

                if (danglingKeywords.Count > 0 || enforceReconnection)
                {
                    // if dangling keywords existed, assign them.
                    connectionGroups.Values
                        .Select(c => c.UserStreamsConnection)
                        .Where(u => u != null)
                        .Where(u => u.TrackKeywords.Count() < UserStreamsConnection.MaxTrackingKeywordCounts || enforceReconnection)
                        .OrderBy(u => u.TrackKeywords.Count())
                        .TakeWhile(_ => danglingKeywords.Count > 0 || enforceReconnection)
                        .ForEach(f =>
                        {
                            var assignable = UserStreamsConnection.MaxTrackingKeywordCounts - f.TrackKeywords.Count();
                            f.TrackKeywords = f.TrackKeywords.Concat(danglingKeywords.Take(assignable));
                            // reconnect this
                            f.Connect();
                            danglingKeywords = danglingKeywords.Skip(assignable).ToList();
                        });
                }
            }
            // dangling keywords should be resolved as null.
            danglingKeywords.ForEach(s => trackResolver[s] = null);
            NotifyDanglings();
        }

        /// <summary>
        /// Notify dangling state.
        /// </summary>
        private static void NotifyDanglings()
        {
            if (danglingKeywords.Count > 0)
            {
                InformationHub.PublishInformation(
                    new Information(InformationKind.Warning,
                        "ConnectionManager_UserStreamsTrackDanglings",
                        "受信されていないトラッキング キーワードがあります。",
                        "トラッキング キーワードに対し、ユーザーストリーム接続数が不足しています。"));
            }
        }

        /// <summary>
        /// Add tracking keywords.
        /// </summary>
        /// <param name="keyword">adding keyword</param>
        /// <param name="reconnectImmediate">reconnect user streams immediately if required.</param>
        public static void AddTrackKeyword(string keyword, bool reconnectImmediate = true)
        {
            lock (trackingLocker)
            {
                if (trackReferenceCount.ContainsKey(keyword))
                {
                    // already registered.
                    trackReferenceCount[keyword]++;
                }
                else
                {
                    trackReferenceCount[keyword] = 1;
                    // connect
                    var connection = GetMostSuitableConnection();
                    if (connection == null)
                    {
                        danglingKeywords.Add(keyword);
                        trackResolver.Add(keyword, null);
                    }
                    else
                    {
                        trackResolver.Add(keyword, connection);
                        connection.TrackKeywords =
                            connection.TrackKeywords.Append(new[] { keyword });
                        if (reconnectImmediate)
                            connection.Connect();
                    }
                }
                NotifyDanglings();
            }
        }

        private static UserStreamsConnection GetMostSuitableConnection()
        {
            lock (connectionGroupsLocker)
            {
                return connectionGroups.Values
                    .Select(c => c.UserStreamsConnection)
                    .Where(u => u != null)
                    .Where(u => u.TrackKeywords.Count() < UserStreamsConnection.MaxTrackingKeywordCounts)
                    .OrderBy(u => u.TrackKeywords.Count())
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Remove tracking keywords.
        /// </summary>
        /// <param name="keyword">removing keyword</param>
        /// <param name="reconnectImmediate">reconnect user streams immediately if required.</param>
        public static void RemoveTrackKeyword(string keyword, bool reconnectImmediate = true)
        {
            lock (trackingLocker)
            {
                if (!trackReferenceCount.ContainsKey(keyword))
                    throw new ArgumentException("Keyword is not registered.");
                if (!trackResolver.ContainsKey(keyword))
                    throw new ArgumentException("Keyword is registered but receiver has not assigned.");
                trackReferenceCount[keyword]--;
                if (trackReferenceCount[keyword] == 0)
                {
                    trackReferenceCount.Remove(keyword);
                    var tracked = trackResolver[keyword];
                    if (tracked != null) // if track-resolver bind key to null value, that's dangling word.
                    {
                        trackResolver.Remove(keyword);
                        tracked.TrackKeywords = tracked.TrackKeywords.Except(new[] { keyword });
                        if (reconnectImmediate)
                            tracked.Connect();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Groups of connection.
    /// </summary>
    internal sealed class ConnectionGroup : IDisposable
    {
        private long userId;
        public long UserId
        {
            get { return userId; }
        }

        private AuthenticateInfo AuthInfo
        {
            get
            {
                return Setting.Accounts.Value
                    .Select(a => a.AuthenticateInfo)
                    .Where(i => i.Id == UserId)
                    .FirstOrDefault();
            }
        }

        private UserTimelinesReceiver receiver;
        public UserTimelinesReceiver EssentialsReceiver
        {
            get { return receiver; }
        }

        public ConnectionGroup(long id)
        {
            this.userId = id;
            receiver = new UserTimelinesReceiver(AuthInfo);
            receiver.IsActivated = true;
        }

        // essential connections
        private UserStreamsConnection userStreams;
        public UserStreamsConnection UserStreamsConnection
        {
            get { return userStreams; }
        }

        public void UserStreamsStartsWith(IEnumerable<string> trackKeywords)
        {
            CheckDispose();
            if (IsUserStreamsEnabled) // already connected
                return;
            userStreams = new UserStreamsConnection(AuthInfo);
            userStreams.IsConnectionAliveEvent += UserStreamsStateChanged;
            userStreams.TrackKeywords = trackKeywords;
            userStreams.Connect();
        }

        private void UserStreamsStateChanged(bool state)
        {
            // TODO: Implementation
        }

        /// <summary>
        /// Get/Set User Streams state.
        /// </summary>
        public bool IsUserStreamsEnabled
        {
            get
            {
                CheckDispose();
                return userStreams != null;
            }
            set
            {
                CheckDispose();
                if (value == IsUserStreamsEnabled) return;
                if (value && AuthInfo != null)
                {
                    // connect
                    userStreams = new UserStreamsConnection(AuthInfo);
                    userStreams.Connect();
                }
                else if (userStreams != null)
                {
                    // disconnect
                    userStreams.Dispose();
                    userStreams = null;
                }
            }
        }

        private bool _isDisposed = false;
        private void CheckDispose()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("ConnectionGroup");
        }

        public void Dispose()
        {
            _isDisposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConnectionGroup()
        {
            if (!_isDisposed)
                Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (userStreams != null)
            {
                var disposal = userStreams;
                userStreams = null;
                disposal.Dispose();
            }
            receiver.Dispose();
        }
    }
}
