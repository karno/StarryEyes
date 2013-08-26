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

namespace StarryEyes.Models.Timeline
{
    public abstract class TimelineModelBase : IDisposable
    {
        public static readonly int TimelineChunkCount = 256;
        public static readonly int TimelineChunkCountBounce = 64;

        public event Action<TwitterStatus> NewStatusArrival;

        protected virtual void OnNewStatusArrival(TwitterStatus obj)
        {
            var handler = this.NewStatusArrival;
            if (handler != null) handler(obj);
        }

        private IDisposable _timelineListener;
        private TimelineModelState _state;
        private readonly object _stateLockObject;
        private FilterQuery _filterQuery;
        private bool _isAutoTrimEnabled;

        private readonly AVLTree<long> _statusIdCache;
        private readonly ObservableSynchronizedCollectionEx<StatusModel> _statuses;

        public bool IsAutoTrimEnabled
        {
            get { return this._isAutoTrimEnabled; }
            set { this._isAutoTrimEnabled = value; }
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
                    if (this._filterQuery != null)
                    {
                        this._filterQuery.Activate();
                    }
                }
                if (previous != null)
                {
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

        public void AcceptStatus(StatusNotification status)
        {
            if (status.IsAdded)
            {
            }
        }

        private async void AddStatus(TwitterStatus status)
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
        }

        private void RemoveStatus(long id)
        {

        }

        private int _trimCount;
        private async Task TrimTimeline()
        {
            if (!_isAutoTrimEnabled) return;
            if (_statuses.Count <= TimelineChunkCount) return;
            try
            {
                StatusModel last;
                if (!_statuses.TryIndexOf(TimelineChunkCount, out last) || last == null) return;
                var lastCreatedAt = last.Status.CreatedAt;
                var removedIds = new List<long>();
                _statuses.RemoveWhere(t =>
                {
                    if (t.Status.CreatedAt < lastCreatedAt)
                    {
                        removedIds.Add(t.Status.Id);
                        return true;
                    }
                    return false;
                });
                lock (_statusIdCache)
                {
                    removedIds.ForEach(id => _statusIdCache.Remove(id));
                }
            }
            finally
            {
                Interlocked.Exchange(ref _trimCount, 0);
            }
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
            // listen timeline
            _timelineListener =
                StatusBroadcaster.BroadcastPoint
                                 .Subscribe(sn =>
                                 {
                                     if (sn.IsAdded)
                                     {
                                         this.AddStatus(sn.Status);
                                     }
                                     else
                                     {
                                         this.RemoveStatus(sn.StatusId);
                                     }
                                 });
            // refresh statuses (with async)
            this.FetchBatch(null, null);
            if (_timelineListener != null)
            {
                throw new InvalidOperationException(
                    "Invalid internal state. (timelineListener must be null in deactivated state.)");
            }

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
