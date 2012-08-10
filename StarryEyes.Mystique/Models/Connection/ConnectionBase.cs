using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
