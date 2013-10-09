using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Models.Backstages.NotificationEvents;

namespace StarryEyes.Models.Receiving.Receivers
{
    public abstract class CyclicReceiverBase : IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        protected CompositeDisposable CompositeDisposable
        {
            get { return this._disposable; }
        }

        private int _remainCountDown;

        private bool _isDisposed;

        public bool IsDisposed
        {
            get { return this._isDisposed; }
        }

        protected abstract int IntervalSec { get; }

        protected CyclicReceiverBase()
        {
            this.CompositeDisposable.Add(Observable.FromEvent(
                h => App.ApplicationFinalize += h,
                h => App.ApplicationFinalize -= h)
                .Subscribe(_ => this.Dispose()));
            this.CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(1))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => this.OnTimer()));
        }

        private void OnTimer()
        {
            if (this._isDisposed) return;
            if (Interlocked.Decrement(ref this._remainCountDown) > 0) return;
            this._remainCountDown = this.IntervalSec;
            try
            {
                this.DoReceive();
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(ex.Message));
            }
        }

        protected abstract void DoReceive();

        protected void CheckDisposed()
        {
            if (this._isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public void Dispose()
        {
            this.CheckDisposed();
            this._isDisposed = true;
            this.Dispose(true);
        }

        ~CyclicReceiverBase()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.CompositeDisposable.Dispose();
        }
    }
}
