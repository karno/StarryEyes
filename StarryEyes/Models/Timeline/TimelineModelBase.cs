using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Albireo.Data;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Receiving.Handling;
using StatusNotification = StarryEyes.Models.Receiving.Handling.StatusNotification;

namespace StarryEyes.Models.Timeline
{
    /// <summary>
    /// 検索フリップなどのタイムラインの基幹部分
    /// </summary>
    public abstract class TimelineModelBase : IDisposable
    {
        public static readonly int TimelineChunkCount = 256;
        public static readonly int TimelineChunkCountBounce = 64;

        private IDisposable _timelineListener;
        private bool _isAutoTrimEnabled;

        private readonly AVLTree<long> _statusIdCache;
        private readonly ObservableSynchronizedCollectionEx<StatusModel> _statuses;

        public bool IsAutoTrimEnabled
        {
            get { return this._isAutoTrimEnabled; }
            set
            {
                if (value == this.IsAutoTrimEnabled)
                {
                    return;
                }
                this._isAutoTrimEnabled = value;
                if (value)
                {
                    this.TrimTimeline();
                }
            }
        }

        public TimelineModelBase()
        {
            _statusIdCache = new AVLTree<long>();
            _statuses = new ObservableSynchronizedCollectionEx<StatusModel>();
        }

        #region Status Global Receiver Control

        /// <summary>
        /// Set this timeline is subscribing global broadcaster
        /// </summary>
        protected bool IsSubscribeBroadcaster
        {
            get { return _timelineListener != null; }
            set
            {
                if (value == this.IsSubscribeBroadcaster) return;
                var ncd = value ? new CompositeDisposable() : null;
                var old = Interlocked.Exchange(ref _timelineListener, ncd);
                if (old != null)
                {
                    old.Dispose();
                }
                if (ncd == null) return;
                // create timeline composition
                ncd.Add(StatusBroadcaster.BroadcastPoint.Subscribe(this.AcceptStatus));
            }
        }

        public void AcceptStatus(StatusNotification n)
        {
            if (n.IsAdded && this.CheckAcceptStatus(n.Status))
            {
                this.AddStatus(n.Status);
            }
            else
            {
                this.RemoveStatus(n.StatusId);
            }
        }

        protected virtual async void AddStatus(TwitterStatus status)
        {
            lock (_statusIdCache)
            {
                if (!_statusIdCache.AddDistinct(status.Id))
                {
                    return;
                }
            }
            // estimate point
            var model = await StatusModel.Get(status);
            var stamp = model.Status.CreatedAt;
            if (this.IsAutoTrimEnabled)
            {
                // check status will not be trimmed
                StatusModel lastModel;
                if (this._statuses.TryIndexOf(TimelineChunkCount, out lastModel) &&
                    lastModel != null &&
                    lastModel.Status.CreatedAt > stamp)
                {
                    // trim target
                    lock (this._statusIdCache)
                    {
                        this._statusIdCache.Remove(model.Status.Id);
                    }
                    return;
                }
            }
            this._statuses.Insert(
                i => i.TakeWhile(s => s.Status.CreatedAt > stamp).Count(),
                model);
            // check auto trim
            if (this.IsAutoTrimEnabled &&
                this._statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                Interlocked.Exchange(ref this._trimCount, 1) == 0)
            {
                this.TrimTimeline();
            }
        }

        protected virtual void RemoveStatus(long id)
        {
            lock (_statusIdCache)
            {
                if (!_statusIdCache.Remove(id)) return;
            }
            // remove
            _statuses.RemoveWhere(s => s.Status.Id == id);
        }

        #endregion

        #region Read more and trimming

        public void ReadMore(long? maxId)
        {
            this.IsAutoTrimEnabled = false;
            this.Fetch(maxId, TimelineChunkCount)
                .Where(this.CheckAcceptStatus)
                .Subscribe(this.AddStatus);
        }

        private int _trimCount;
        private void TrimTimeline()
        {
            Task.Run(() =>
            {
                if (!this._isAutoTrimEnabled) return;
                if (this._statuses.Count <= TimelineChunkCount) return;
                try
                {
                    StatusModel last;
                    if (!this._statuses.TryIndexOf(TimelineChunkCount, out last) || last == null) return;
                    var lastCreatedAt = last.Status.CreatedAt;
                    var removedIds = new List<long>();
                    this._statuses.RemoveWhere(t =>
                    {
                        if (t.Status.CreatedAt < lastCreatedAt)
                        {
                            removedIds.Add(t.Status.Id);
                            return true;
                        }
                        return false;
                    });
                    lock (this._statusIdCache)
                    {
                        removedIds.ForEach(id => this._statusIdCache.Remove(id));
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref this._trimCount, 0);
                }
            });
        }

        #endregion

        #region Invalidate whole timeline

        private const int InvalidateSec = 5;
        private int _invlatch;
        protected void QueueInvalidateTimeline()
        {
            var stamp = Interlocked.Increment(ref _invlatch);
            Observable.Timer(TimeSpan.FromSeconds(InvalidateSec))
                      .Subscribe(_ =>
                      {
                          if (Interlocked.CompareExchange(ref _invlatch, 0, stamp) == stamp)
                          {
                              Task.Run(() => this.InvalidateTimeline());
                          }
                      });
        }

        private Task InvalidateTimeline()
        {
            return Task.Run(() =>
            {
                this.PreInvalidateTimeline();
                // invalidate and fetch statuses
                lock (_statusIdCache)
                {
                    _statusIdCache.Clear();
                    _statuses.Clear();
                }
                this.Fetch(null, TimelineChunkCount)
                    .Subscribe(s => this.AcceptStatus(new StatusNotification(s)));
            });
        }

        #endregion

        #region Abstract Methods

        protected abstract void PreInvalidateTimeline();

        protected abstract bool CheckAcceptStatus(TwitterStatus status);

        protected abstract IObservable<TwitterStatus> Fetch(long? maxId, int? count);

        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~TimelineModelBase()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            IsSubscribeBroadcaster = false;
            lock (_statusIdCache)
            {
                _statusIdCache.Clear();
            }
            _statuses.Clear();
        }

        protected enum TimelineModelState
        {
            Activated,
            Deactivated,
            Disposed,
        }
    }
}
