using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarryEyes.Mystique.Models.Connection.Continuous;
using System.Reactive.Disposables;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.Mystique.Settings;

namespace StarryEyes.Mystique.Models.Connection
{
    /// <summary>
    /// Provides management for connect to twitter.
    /// </summary>
    public static class ConnectionManager
    {
        private static object connectionGroupsLocker = new object();
        private static SortedDictionary<long, ConnectionGroup> connectionGroups =
            new SortedDictionary<long, ConnectionGroup>();

        private static object trackingLocker = new object();
        private static SortedDictionary<string, UserStreamsConnection> trackResolver =
            new SortedDictionary<string, UserStreamsConnection>();
        private static SortedDictionary<string, int> trackReferenceCount =
            new SortedDictionary<string, int>();

        /// <summary>
        /// Update connection states.<para />
        /// Apply current setting params, maintain the connections.
        /// </summary>
        /// <param name="enforceReconnection">all user streams connection will be reconnected with enforced</param>
        public static void Update(bool enforceReconnection = false)
        {
        }

        /// <summary>
        /// Add tracking keywords.
        /// </summary>
        /// <param name="keyword">adding keyword</param>
        /// <param name="reconnectImmediate">reconnect user streams immediately if required.</param>
        /// <returns>if successfully added, returns true.</returns>
        public static bool AddTrackKeyword(string keyword, bool reconnectImmediate = true)
        {
            lock (trackingLocker)
            {
                if (trackReferenceCount.ContainsKey(keyword))
                {
                    // already registered.
                    trackReferenceCount[keyword]++;
                    return true;
                }
                else
                {
                    trackReferenceCount[keyword] = 1;
                    // connect
                    var connection = GetMostSuitableConnection();
                    if (connection == null)
                        return false;
                    trackResolver.Add(keyword, connection);
                    connection.TrackKeywords =
                        connection.TrackKeywords.Append(new[] { keyword });
                    if (reconnectImmediate)
                        connection.Connect();
                    return true;
                }
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
                    trackResolver.Remove(keyword);
                    tracked.TrackKeywords = tracked.TrackKeywords.Except(new[] { keyword });
                    if (reconnectImmediate)
                        tracked.Connect();
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

        public ConnectionGroup(long id)
        {
            this.userId = id;
        }

        // essential connections
        private UserStreamsConnection userStreams;
        public UserStreamsConnection UserStreamsConnection
        {
            get { return userStreams; }
        }

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
            CheckDispose();
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
                userStreams.Dispose();
        }
    }
}
