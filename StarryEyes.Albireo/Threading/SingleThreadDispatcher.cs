using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Albireo.Threading
{
    public class SingleThreadDispatcher : IDisposable
    {
        private readonly Queue<Action> _operations;
        private readonly Thread _dispatchThread;
        private readonly object _queueLocker = new object();
        private bool _isThreadAlive = true;
        private bool _disposed;

        public SingleThreadDispatcher()
        {
            _operations = new Queue<Action>();
            _dispatchThread = new Thread(Worker);
            _dispatchThread.Start();
        }

        private void Worker()
        {
            while (_isThreadAlive)
            {
                Action current = null;
                lock (_queueLocker)
                {
                    if (_operations.Count > 0)
                    {
                        current = _operations.Dequeue();
                    }
                    else
                    {
                        Monitor.Wait(_queueLocker);
                    }
                }
                if (current != null)
                {
                    current();
                }
            }
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SingleThreadDispatcher is already disposed.");
            }
        }

        public async Task<T> Dispatch<T>(Func<T> exec, CancellationToken cancellationToken = default(CancellationToken))
        {
            var composite = new CompositeDisposable();
            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(composite.Dispose);
            }
            var subj = new Subject<T>();
            composite.Add(subj);
            var act = new Action(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    composite.Add(
                        Observable.Defer(() => Observable.Return(new Unit()))
                                  .Select(_ => exec())
                                  .Subscribe(subj));
                }
            });
            lock (_queueLocker)
            {
                _operations.Enqueue(act);
                Monitor.Pulse(_queueLocker);
            }
            return await subj.FirstAsync();
        }

        public async Task Dispatch(Action exec, CancellationToken cancellationToken = default (CancellationToken))
        {
            var _ = await Dispatch(() =>
            {
                exec();
                return Unit.Default;
            });
        }

        public void Dispose()
        {
            CheckDisposed();
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SingleThreadDispatcher()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
            if (disposing)
            {
                _isThreadAlive = false;
            }
        }
    }
}
