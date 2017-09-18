using System;
using System.Collections.Generic;
using Sophia.Messaging.Core;

namespace Sophia
{
    public abstract class ViewModel : NotificationObject, IDisposable
    {
        private bool _disposed;

        private readonly List<IDisposable> _disposables;

        public Messenger Messenger { get; } = new Messenger();

        protected ViewModel()
        {
            _disposables = new List<IDisposable> { Messenger };
        }

        public void AddDisposable(IDisposable disposable)
        {
            lock (_disposables)
            {
                if (_disposed)
                {
                    disposable.Dispose();
                }
                else
                {
                    _disposables.Add(disposable);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ViewModel()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (disposing)
            {
                lock (_disposables)
                {
                    foreach (var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                    _disposables.Clear();
                }
            }
        }
    }
}