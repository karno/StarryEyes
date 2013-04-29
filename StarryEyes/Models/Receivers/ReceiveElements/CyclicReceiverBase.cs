using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public abstract class CyclicReceiverBase : IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        protected CompositeDisposable CompositeDisposable
        {
            get { return _disposable; }
        }

        private int _remainCountDown;

        private bool _isDisposed;

        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        protected abstract int IntervalSec { get; }

        protected CyclicReceiverBase()
        {
            CompositeDisposable.Add(Observable.FromEvent(
                h => App.ApplicationFinalize += h,
                h => App.ApplicationFinalize -= h)
                .Subscribe(_ => this.Dispose()));
            CompositeDisposable.Add(Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => OnTimer()));
        }

        private void OnTimer()
        {
            if (_isDisposed) return;
            if (Interlocked.Decrement(ref _remainCountDown) > 0) return;
            _remainCountDown = IntervalSec;
            DoReceive();
        }

        protected abstract void DoReceive();

        protected void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public void Dispose()
        {
            CheckDisposed();
            _isDisposed = true;
            Dispose(true);
        }

        ~CyclicReceiverBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            CompositeDisposable.Dispose();
        }
    }
}
