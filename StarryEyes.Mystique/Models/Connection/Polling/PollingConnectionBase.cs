using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Authorize;

namespace StarryEyes.Mystique.Models.Connection.Polling
{
    /// <summary>
    /// Periodically receiving task
    /// </summary>
    public abstract class PollingConnectionBase : ConnectionBase
    {
        /// <summary>
        /// internal disposables holder
        /// </summary>
        private CompositeDisposable _disposablesHolder = new CompositeDisposable();

        private int _currentTick = 0;

        public PollingConnectionBase(AuthenticateInfo ai)
            : base(ai)
        {
            _disposablesHolder.Add(Observable.FromEvent(
                _ => App.OnApplicationFinalize += _,
                _ => App.OnApplicationFinalize -= _)
                .Subscribe(_ => this.Dispose()));
            _disposablesHolder.Add(Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => OnTick()));
        }

        /// <summary>
        /// On time callback
        /// </summary>
        private void OnTick()
        {
            if (!IsActivated) return;
            _currentTick++;
            if (_currentTick > IntervalSec)
            {
                _currentTick = 0;
                DoReceive();
            }
        }

        protected abstract int IntervalSec { get; }

        protected abstract void DoReceive();

        /// <summary>
        /// Set whether this connection is activated.
        /// </summary>
        public bool IsActivated { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _disposablesHolder.Dispose();
        }
    }
}
