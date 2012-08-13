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
    public static class ConnectionManager
    {
        private static SortedDictionary<long, ConnectionGroup> connectionGroups;

        private static SortedDictionary<string, LinkedList<UserStreamsConnection>> trackResolver;

        private static SortedDictionary<string, int> trackReferenceCount;

        /// <summary>
        /// Update connection states.<para />
        /// Apply current setting params, maintain the connections.
        /// </summary>
        public static void Update()
        {
        }
    }

    public sealed class ConnectionGroup : IDisposable
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
