using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Globalization;
using StarryEyes.Globalization.Models;
using StarryEyes.Models.Backstages.NotificationEvents;

namespace StarryEyes.Models.Receiving.Receivers
{
    public abstract class CyclicReceiverBase : IDisposable
    {
        private int _remainCountDown;

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        protected CompositeDisposable CompositeDisposable
        {
            get { return _disposable; }
        }

        private bool _isDisposed;
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        protected abstract string ReceiverName { get; }

        protected abstract int IntervalSec { get; }

        protected CyclicReceiverBase()
        {
            CompositeDisposable.Add(Observable.FromEvent(
                h => App.ApplicationFinalize += h,
                h => App.ApplicationFinalize -= h)
                .Subscribe(_ => Dispose()));
            CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(1))
                          .ObserveOn(TaskPoolScheduler.Default)
                          .Subscribe(_ => OnTimer()));
        }

        private async void OnTimer()
        {
            if (_isDisposed) return;
            if (Interlocked.Decrement(ref _remainCountDown) > 0) return;
            _remainCountDown = IntervalSec;
            try
            {
                await Task.Run(async () =>
                {
                    using (MainWindowModel.SetState(ReceivingResources.ReceivingFormat.SafeFormat(ReceiverName)))
                    {
                        await DoReceive().ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                BackstageModel.RegisterEvent(new OperationFailedEvent(
                    ReceivingResources.ReceiveFailedFormat.SafeFormat(ReceiverName),
                    ex));
            }
        }

        protected abstract Task DoReceive();

        protected void AssertDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
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
