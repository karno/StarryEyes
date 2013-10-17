using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Albireo.Collections;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Timelines
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

        public event Action<bool> InvalidationStateChanged;

        protected virtual void OnInvalidationStateChanged(bool obj)
        {
            var handler = this.InvalidationStateChanged;
            if (handler != null) handler(obj);
        }

        public ObservableSynchronizedCollectionEx<StatusModel> Statuses
        {
            get { return this._statuses; }
        }

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
            this._statusIdCache = new AVLTree<long>();
            this._statuses = new ObservableSynchronizedCollectionEx<StatusModel>();
        }

        #region Status Global Receiver Control

        /// <summary>
        /// Set this timeline is subscribing global broadcaster
        /// </summary>
        protected bool IsSubscribeBroadcaster
        {
            get { return this._timelineListener != null; }
            set
            {
                if (value == this.IsSubscribeBroadcaster) return;
                var ncd = value ? new CompositeDisposable() : null;
                var old = Interlocked.Exchange(ref this._timelineListener, ncd);
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
#pragma warning disable 4014
                this.AddStatus(n.Status, true);
#pragma warning restore 4014
            }
            else
            {
                this.RemoveStatus(n.StatusId);
            }
        }

        protected virtual async Task<bool> AddStatus(TwitterStatus status, bool isNewArrival)
        {
            lock (this._statusIdCache)
            {
                if (this._statusIdCache.Contains(status.Id))
                {
                    return false;
                }
            }
            // estimate point
            var model = await StatusModel.Get(status);
            var stamp = model.Status.CreatedAt;
            if (this.IsAutoTrimEnabled)
            {
                // check status will not be trimmed
                StatusModel lastModel;
                if (this.Statuses.TryIndexOf(TimelineChunkCount, out lastModel) &&
                    lastModel != null &&
                    lastModel.Status.CreatedAt > stamp)
                {
                    // status is in below of trim offset
                    return false;
                }
            }
            lock (this._statusIdCache)
            {
                if (!this._statusIdCache.AddDistinct(status.Id))
                {
                    return false;
                }
            }
            this.Statuses.Insert(
                i => i.TakeWhile(s => s.Status.CreatedAt > stamp).Count(),
                model);
            // check auto trim
            if (this.IsAutoTrimEnabled &&
                this._statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                Interlocked.Exchange(ref this._trimCount, 1) == 0)
            {
                this.TrimTimeline();
            }
            return true;
        }

        protected virtual void RemoveStatus(long id)
        {
            lock (this._statusIdCache)
            {
                if (!this._statusIdCache.Remove(id)) return;
            }
            // remove
            this.Statuses.RemoveWhere(s => s.Status.Id == id);
        }

        #endregion

        #region Read more and trimming

        public async Task ReadMore(long? maxId)
        {
            await this.Fetch(maxId, TimelineChunkCount)
                      .Where(this.CheckAcceptStatus)
                      .SelectMany(s => this.AddStatus(s, false).ToObservable())
                      .LastOrDefaultAsync();
        }

        private int _trimCount;
        private void TrimTimeline()
        {
            Task.Run(() =>
            {
                if (!this._isAutoTrimEnabled) return;
                if (this.Statuses.Count <= TimelineChunkCount) return;
                try
                {
                    StatusModel last;
                    if (!this.Statuses.TryIndexOf(TimelineChunkCount, out last) || last == null) return;
                    var lastCreatedAt = last.Status.CreatedAt;
                    var removedIds = new List<long>();
                    this.Statuses.RemoveWhere(t =>
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

        private const int InvalidateSec = 2;
        private int _invlatch;

        internal void QueueInvalidateTimeline()
        {
            var stamp = Interlocked.Increment(ref this._invlatch);
            Observable.Timer(TimeSpan.FromSeconds(InvalidateSec))
                      .Subscribe(_ =>
                      {
                          if (Interlocked.CompareExchange(ref this._invlatch, 0, stamp) == stamp)
                          {
                              this.InvalidateTimeline();
                          }
                      });
        }

        public void InvalidateTimeline()
        {
            Task.Run(async () =>
            {
                this.OnInvalidationStateChanged(true);
                this.PreInvalidateTimeline();
                // invalidate and fetch statuses
                lock (this._statusIdCache)
                {
                    this._statusIdCache.Clear();
                    this.Statuses.Clear();
                }
                await this.Fetch(null, TimelineChunkCount)
                          .Do(s => this.AcceptStatus(new StatusNotification(s)))
                          .LastOrDefaultAsync();
                this.OnInvalidationStateChanged(false);
            });
        }

        #endregion

        #region Focusing

        public event Action FocusRequired;

        public void RequestFocus()
        {
            var handler = FocusRequired;
            if (handler != null) handler();
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
            this.IsSubscribeBroadcaster = false;
            lock (this._statusIdCache)
            {
                this._statusIdCache.Clear();
            }
            this.Statuses.Clear();
        }

        protected enum TimelineModelState
        {
            Activated,
            Deactivated,
            Disposed,
        }
    }
}
