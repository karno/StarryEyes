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
    public class SingleThreadRunner : IDisposable
    {
        private readonly LinkedList<Action> _operations;
        private readonly Thread _dispatchThread;
        private readonly object _queueLocker = new object();
        private bool _isThreadAlive = true;
        private bool _disposed;

        public SingleThreadRunner()
        {
            _operations = new LinkedList<Action>();
            _dispatchThread = new Thread(Worker);
            _dispatchThread.Start();
        }

        private void Worker()
        {
            try
            {
                while (_isThreadAlive)
                {
                    Action current = null;
                    lock (_queueLocker)
                    {
                        if (_operations.Count > 0)
                        {
                            current = _operations.First.Value;
                            _operations.RemoveFirst();
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
            catch (ThreadAbortException) { }
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SingleThreadRunner is already disposed.");
            }
        }

        public async Task<T> Enqueue<T>(Func<T> exec,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.AddItem(exec, false, cancellationToken);
        }

        public async Task Enqueue(Action exec,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            await this.Enqueue(() =>
           {
               exec();
               return Unit.Default;
           }, cancellationToken);
        }

        public async Task<T> Push<T>(Func<T> exec,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            return await this.AddItem(exec, true, cancellationToken);
        }

        public async Task Push(Action exec,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            await this.Push(() =>
            {
                exec();
                return Unit.Default;
            }, cancellationToken);
        }

        private async Task<T> AddItem<T>(Func<T> exec, bool addFirst,
            CancellationToken cancellationToken)
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
                if (addFirst)
                {
                    _operations.AddFirst(act);
                }
                else
                {
                    _operations.AddLast(act);
                }
                Monitor.Pulse(_queueLocker);
            }
            return await subj.FirstAsync();
        }

        public void Dispose()
        {
            CheckDisposed();
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SingleThreadRunner()
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
            _dispatchThread.Abort();
        }
    }
}
