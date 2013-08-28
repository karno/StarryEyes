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
using StarryEyes.Filters;
using StarryEyes.Models.Statuses;
using StatusNotification = StarryEyes.Models.Statuses.StatusNotification;

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
        private TimelineModelState _state;
        private readonly object _stateLockObject;
        private FilterQuery _filterQuery;
        private Func<TwitterStatus, bool> _filterFunc;
        private string _filterSql;
        private bool _isAutoTrimEnabled;

        private readonly AVLTree<long> _statusIdCache;
        private readonly ObservableSynchronizedCollectionEx<StatusModel> _statuses;

        protected Func<TwitterStatus, bool> FilterFunc { get { return _filterFunc; } }

        protected string FilterSql { get { return _filterSql; } }

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

        public FilterQuery FilterQuery
        {
            get { return _filterQuery; }
            set
            {
                FilterQuery previous;
                lock (_stateLockObject)
                {
                    previous = _filterQuery;
                    _filterQuery = value;
                    if (this._state != TimelineModelState.Activated) return;
                    if (value != null)
                    {
                        value.Activate();
                        value.InvalidateRequired += this.QueueInvalidateBatch;
                    }
                }
                if (previous != null)
                {
                    previous.InvalidateRequired -= this.QueueInvalidateBatch;
                    previous.Deactivate();
                }
            }
        }

        public TimelineModelBase()
        {
            _statusIdCache = new AVLTree<long>();
            _statuses = new ObservableSynchronizedCollectionEx<StatusModel>();
            _stateLockObject = new object();
            _state = TimelineModelState.Deactivated;
        }

        public TimelineModelBase(FilterQuery initiateFilter)
            : this()
        {
            FilterQuery = initiateFilter;
        }

        public IDisposable GetBindTicket()
        {
            this.Activate();
            return Disposable.Create(() => Task.Run(() => this.Deactivate()));
        }

        public void AcceptStatus(StatusNotification n)
        {
            var ffn = _filterFunc;
            if (n.IsAdded && ffn != null && ffn(n.Status))
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

        private const int InvalidateSec = 5;
        private int _invlatch;
        private void QueueInvalidateBatch()
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

        protected Task InvalidateTimeline()
        {
            return Task.Run(async () =>
            {
                var fq = _filterQuery;
                // invalidate filter query
                if (fq != null)
                {
                    _filterFunc = fq.GetEvaluator();
                    this._filterSql = fq.PredicateTreeRoot != null
                                          ? fq.PredicateTreeRoot.GetSqlQuery()
                                          : null;
                }
                else
                {
                    _filterFunc = _ => false;
                    _filterSql = null;
                }
                // invalidate and fetch statuses
                lock (_statusIdCache)
                {
                    _statusIdCache.Clear();
                    _statuses.Clear();
                }
                var statuses = await this.FetchBatch(null, null);
                statuses.ForEach(s => this.AcceptStatus(new StatusNotification(s)));
            });
        }

        protected abstract IObservable<TwitterStatus> Fetch(long? maxId, int? count);

        protected virtual Task<IEnumerable<TwitterStatus>> FetchBatch(long? maxId, int? count)
        {
            return Task.Run(async () =>
            {
                var statuses = new List<TwitterStatus>();
                await this.Fetch(maxId, count)
                          .Do(statuses.Add)
                          .LastOrDefaultAsync().GetAwaiter();
                return statuses.AsEnumerable();
            });
        }

        #region State Control

        protected void Activate()
        {
            this.AssertDisposed();
            lock (_stateLockObject)
            {
                if (_state == TimelineModelState.Activated)
                {
                    throw new InvalidOperationException("This timeline is already bound.");
                }
                this.ActivateCore();
            }
        }

        protected void Deactivate()
        {
            lock (_stateLockObject)
            {
                if (_state == TimelineModelState.Activated)
                {
                    this._filterQuery.Deactivate();
                }
            }
        }

        // Below method should call in lock(_stateLockObject).

        private void ActivateCore()
        {
            // activate base properties
            _state = TimelineModelState.Activated;
            this._filterQuery.Activate();
            lock (_statusIdCache)
            {
                _statusIdCache.Clear();
                _statuses.Clear();
            }
            if (_timelineListener != null)
            {
                throw new InvalidOperationException(
                    "Invalid internal state. (timelineListener must be null in deactivated state.)");
            }
            // listen timeline
            _timelineListener = StatusBroadcaster.BroadcastPoint
                                                 .Subscribe(this.AcceptStatus);
            // refresh statuses (with async)
            Task.Run(() => this.InvalidateTimeline());
        }

        private void DeactivateCore()
        {
            // deactivate basis
            _state = TimelineModelState.Deactivated;
            this._filterQuery.Deactivate();
            if (_timelineListener == null)
            {
                throw new InvalidOperationException(
                    "Invalid internal state. (timelineListener must not be null in activated state.)");
            }
            _timelineListener.Dispose();
            _timelineListener = null;
        }

        private void DisposeCore()
        {
            this.AssertDisposed();
            var prevState = this._state;
            this._state = TimelineModelState.Disposed;
            if (prevState == TimelineModelState.Activated)
            {
                this.DeactivateCore();
            }
        }

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
            this.DisposeCore();
        }

        protected void AssertDisposed()
        {
            if (this._state == TimelineModelState.Disposed)
            {
                throw new ObjectDisposedException("TimelineViewModelBase");
            }
        }

        protected enum TimelineModelState
        {
            Activated,
            Deactivated,
            Disposed,
        }
    }
}
