using System;
using JetBrains.Annotations;

namespace Sophia.Utilities
{
    public static class Disposable
    {
        public static IDisposable Create(Action disposeAction, bool allowOnce = false)
        {
            return new AnonymousDisposable(disposeAction, allowOnce);
        }

        private sealed class AnonymousDisposable : IDisposable
        {
            private bool _disposed;
            private readonly Action _disposer;
            private readonly bool _allowOnce;

            public AnonymousDisposable([NotNull] Action disposer, bool allowOnce)
            {
                if (disposer == null) throw new ArgumentNullException(nameof(disposer));
                _disposer = disposer;
                _allowOnce = allowOnce;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _disposer();
                }
                else if (_allowOnce)
                {
                    throw new ObjectDisposedException("This object is already disposed.");
                }
            }
        }
    }
}