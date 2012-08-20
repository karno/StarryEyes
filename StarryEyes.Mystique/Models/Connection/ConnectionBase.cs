using System;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection
{
    public abstract class ConnectionBase : IDisposable
    {
        private AuthenticateInfo _authInfo;
        protected AuthenticateInfo AuthInfo
        {
            get { return _authInfo; }
        }

        protected void RaiseInfoNotification(string id, string abst, string description, bool isWarning = false)
        {
            InformationHub.PublishInformation(
                new Information(isWarning ? InformationKind.Warning : InformationKind.Notify, id,
                    "@" + _authInfo.UnreliableScreenName + " - " + abst, description));
        }

        protected void RaiseErrorNotification(string id, string abst, string desc, string fixName, Action fix)
        {
            InformationHub.PublishInformation(
                new Information(InformationKind.Error, id,
                    "@" + _authInfo.UnreliableScreenName + " - " + abst, desc,
                    fixName, fix));
        }

        public ConnectionBase(AuthenticateInfo authInfo)
        {
            this._authInfo = authInfo;
        }

        bool _disposed = false;
        protected bool IsDisposed
        {
            get { return _disposed; }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        ~ConnectionBase()
        {
            if (!_disposed)
                Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }
    }
}
