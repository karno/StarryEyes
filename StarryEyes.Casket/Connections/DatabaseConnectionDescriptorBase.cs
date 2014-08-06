using System;
using System.Data.Common;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Albireo.Threading;

namespace StarryEyes.Casket.Connections
{
    public abstract class DatabaseConnectionDescriptorBase : IDatabaseConnectionDescriptor
    {
        #region concurrent access manager

        private static readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private static readonly TaskFactory _readTaskFactory = LimitedTaskScheduler.GetTaskFactory(8);

        private static readonly TaskFactory _writeTaskFactory = LimitedTaskScheduler.GetTaskFactory(1);

        #endregion

        private bool _disposed;

        public TaskFactory GetTaskFactory(bool isWrite)
        {
            AssertDisposed();
            return isWrite ? _writeTaskFactory : _readTaskFactory;
        }

        public IDisposable AcquireWriteLock()
        {
            AssertDisposed();
            _rwlock.EnterWriteLock();
            return Disposable.Create(() => _rwlock.ExitWriteLock());
        }

        public IDisposable AcquireReadLock()
        {
            AssertDisposed();
            _rwlock.EnterReadLock();
            return Disposable.Create(() => _rwlock.ExitReadLock());
        }

        public DbConnection GetConnection()
        {
            AssertDisposed();
#if DEBUG
            if (!_rwlock.IsReadLockHeld &&
                !_rwlock.IsUpgradeableReadLockHeld &&
                !_rwlock.IsWriteLockHeld)
            {
                throw new InvalidOperationException("This thread does not have any locks!");
            }
#endif
            return CreateConnectionCore();
        }

        protected abstract DbConnection CreateConnectionCore();

        private void AssertDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DatabaseConnectionDescriptor");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DatabaseConnectionDescriptorBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            this._disposed = true;
        }
    }
}
