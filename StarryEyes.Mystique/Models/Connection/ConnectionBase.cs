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

        protected void RaiseInfoNotification(string abst, string description, bool isWarning = false)
        {
            InformationHub.PublishInformation(
                new Information(isWarning ? InformationKind.Warning : InformationKind.Notify,
                    "@" + _authInfo.UnreliableScreenName + " - " + abst, description));
        }

        protected void RaiseErrorNotification(string abst, string desc, string fixName, Action fix)
        {
            InformationHub.PublishInformation(
                new Information(InformationKind.Error,
                    "@" + _authInfo.UnreliableScreenName + " - " + abst, desc,
                    fixName, fix));
        }

        public ConnectionBase(AuthenticateInfo authInfo)
        {
            this._authInfo = authInfo;
        }

        bool _disposed = false;
        public void Dispose()
        {
            CheckDisposed();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConnectionBase()
        {
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
