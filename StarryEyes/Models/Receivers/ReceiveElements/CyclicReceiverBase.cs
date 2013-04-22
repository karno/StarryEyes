using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StarryEyes.Models.Receivers.ReceiveElements
{
    public abstract class CyclicReceiverBase : IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        protected CompositeDisposable CompositeDisposable
        {
            get { return _disposable; }
        }

        private int _currentSec;

        private bool _isDisposed;

        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        protected abstract int IntervalSec { get; }

        protected CyclicReceiverBase()
        {
            CompositeDisposable.Add(Observable.FromEvent(
                h => App.OnApplicationFinalize += h,
                h => App.OnApplicationFinalize -= h)
                .Subscribe(_ => this.Dispose()));
            CompositeDisposable.Add(Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => OnTimer()));
            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _currentSec = IntervalSec; // first receive occurs immediately.
        }

        private void OnTimer()
        {
            _currentSec++;
            if (_currentSec < IntervalSec) return;
            _currentSec = 0;
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
