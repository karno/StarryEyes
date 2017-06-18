using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Starcluster.Threading;

namespace Starcluster.Connections
{
    public abstract class DatabaseConnectionDescriptorBase : IDatabaseConnectionDescriptor
    {
        #region concurrent access manager

        private static readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private static readonly TaskFactory _readTaskFactory = LimitedTaskScheduler.GetTaskFactory(8);

        private static readonly TaskFactory _writeTaskFactory = LimitedTaskScheduler.GetTaskFactory(1);

        #endregion concurrent access manager

        private bool _disposed;

        public TaskFactory GetTaskFactory(bool isWrite)
        {
            AssertNotDisposed();
            return isWrite ? _writeTaskFactory : _readTaskFactory;
        }

        public IDisposable AcquireWriteLock()
        {
            AssertNotDisposed();
            _rwlock.EnterWriteLock();
            return new ExitWriteLockDisposable(_rwlock);
        }

        public IDisposable AcquireReadLock()
        {
            AssertNotDisposed();
            _rwlock.EnterReadLock();
            return new ExitReadLockDisposable(_rwlock);
        }

        public IDbConnection GetConnection()
        {
            AssertNotDisposed();
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

        protected abstract IDbConnection CreateConnectionCore();

        private void AssertNotDisposed()
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
            _disposed = true;
        }

        private sealed class ExitWriteLockDisposable : IDisposable
        {
            private readonly ReaderWriterLockSlim _locker;
            private bool _exited;

            public ExitWriteLockDisposable(ReaderWriterLockSlim locker)
            {
                _locker = locker;
            }

            public void Dispose()
            {
                lock (_locker)
                {
                    if (_exited) return;
                    _exited = true;
                    _locker.ExitWriteLock();
                }
            }
        }

        private sealed class ExitReadLockDisposable : IDisposable
        {
            private readonly ReaderWriterLockSlim _locker;
            private bool _exited;

            public ExitReadLockDisposable(ReaderWriterLockSlim locker)
            {
                _locker = locker;
            }

            public void Dispose()
            {
                lock (_locker)
                {
                    if (_exited) return;
                    _exited = true;
                    _locker.ExitReadLock();
                }
            }
        }
    }
}