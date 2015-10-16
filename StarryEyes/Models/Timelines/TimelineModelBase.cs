using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Timelines
{
    /// <summary>
    /// Base of timeline
    /// </summary>
    public abstract class TimelineModelBase : IDisposable
    {
        public static readonly int TimelineChunkCount = 256;
        public static readonly int TimelineChunkCountBounce = 64;

        private IDisposable _timelineListener;
        private bool _isAutoTrimEnabled;
        private bool _isLoading;

        private readonly HashSet<long> _statusIdCache;
        private readonly ObservableSynchronizedCollectionEx<StatusModel> _statuses;

        public bool IsLoading
        {
            get { return _isLoading; }
            protected set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                IsLoadingChanged.SafeInvoke(value);
            }
        }

        public event Action<bool> IsLoadingChanged;

        public ObservableSynchronizedCollectionEx<StatusModel> Statuses
        {
            get { return _statuses; }
        }

        public bool IsAutoTrimEnabled
        {
            get { return _isAutoTrimEnabled; }
            set
            {
                if (value == IsAutoTrimEnabled)
                {
                    return;
                }
                _isAutoTrimEnabled = value;
                if (value)
                {
                    TrimTimeline();
                }
            }
        }

        public TimelineModelBase()
        {
            _statusIdCache = new HashSet<long>();
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
                if (value == IsSubscribeBroadcaster) return;
                var ncd = value ? new CompositeDisposable() : null;
                var old = Interlocked.Exchange(ref _timelineListener, ncd);
                if (old != null)
                {
                    old.Dispose();
                }
                if (ncd == null) return;
                // create timeline composition
                ncd.Add(StatusBroadcaster.BroadcastPoint.Subscribe(AcceptStatus));
            }
        }

        private void AcceptStatus(StatusModelNotification n)
        {
            if (n.IsAdded && n.StatusModel != null && CheckAcceptStatus(n.StatusModel.Status))
            {
                AddStatus(n.StatusModel, n.IsNew);
            }
            else
            {
                RemoveStatus(n.StatusId);
            }
        }

        private async Task<bool> AddStatus(TwitterStatus status, bool isNewArrival)
        {
            if (!CheckStatusAdd(status, false))
            {
                return false;
            }

            var model = await StatusModel.Get(status).ConfigureAwait(false);
            return AddStatus(model, isNewArrival);
        }

        protected virtual bool AddStatus(StatusModel model, bool isNewArrival)
        {
            if (!CheckStatusAdd(model.Status, true))
            {
                return false;
            }

            var stamp = model.Status.CreatedAt;
            Statuses.Insert(
                i => i.TakeWhile(s => s.Status.CreatedAt > stamp).Count(),
                model);
            // check auto trim
            if (IsAutoTrimEnabled &&
                _statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                Interlocked.Exchange(ref _trimCount, 1) == 0)
            {
                TrimTimeline();
            }
            return true;
        }

        private bool CheckStatusAdd(TwitterStatus status, bool actualAdd)
        {
            var stamp = status.CreatedAt;
            if (IsAutoTrimEnabled)
            {
                // check whether status is in trimmed place or not.
                StatusModel lastModel;
                if (Statuses.TryIndexOf(TimelineChunkCount, out lastModel) &&
                    lastModel != null &&
                    lastModel.Status.CreatedAt > stamp)
                {
                    // status is in below of trim offset
                    return false;
                }
            }
            lock (_statusIdCache)
            {
                if (actualAdd
                    ? !_statusIdCache.Add(status.Id)
                    : _statusIdCache.Contains(status.Id))
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void RemoveStatus(long id)
        {
            lock (_statusIdCache)
            {
                if (!_statusIdCache.Remove(id)) return;
            }
            // remove
            Statuses.RemoveWhere(s => s.Status.Id == id);
        }

        #endregion

        #region Read more and trimming

        public async Task ReadMore(long? maxId)
        {
            await ReadMore(maxId, true).ConfigureAwait(false);
        }

        private async Task ReadMore(long? maxId, bool setLoadingFlag)
        {
            if (setLoadingFlag)
            {
                IsLoading = true;
            }
            await Fetch(maxId, TimelineChunkCount)
                      .Where(CheckAcceptStatus)
                      .Select(s => AddStatus(s, false))
                      .LastOrDefaultAsync();
            if (setLoadingFlag)
            {
                IsLoading = false;
            }
        }

        private int _trimCount;
        private void TrimTimeline()
        {
            Task.Run(() =>
            {
                if (!_isAutoTrimEnabled) return;
                if (Statuses.Count <= TimelineChunkCount) return;
                try
                {
                    StatusModel last;
                    if (!Statuses.TryIndexOf(TimelineChunkCount, out last) || last == null) return;
                    var lastCreatedAt = last.Status.CreatedAt;
                    var removedIds = new List<long>();
                    Statuses.RemoveWhere(t =>
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
            });
        }

        #endregion

        #region Invalidate whole timeline

        private const int QueueInvalidationWaitSec = 2;
        private int _invlatch;

        protected void QueueInvalidateTimeline()
        {
            var stamp = Interlocked.Increment(ref _invlatch);
            Observable.Timer(TimeSpan.FromSeconds(QueueInvalidationWaitSec))
                      .Subscribe(_ =>
                      {
                          if (Interlocked.CompareExchange(ref _invlatch, 0, stamp) == stamp)
                          {
                              InvalidateTimeline();
                          }
                      });
        }

        public void InvalidateTimeline()
        {
            // clear queued invalidates
            Interlocked.Increment(ref _invlatch);
            Task.Run(async () =>
            {
                var complete = false;
                try
                {
                    IsLoading = true;
                    if (PreInvalidateTimeline())
                    {
                        // when PreInvalidateTimeline is finished correctly,
                        // reserve to down loading flag.
                        complete = true;
                    }
                    // invalidate and fetch statuses
                    lock (_statusIdCache)
                    {
                        _statusIdCache.Clear();
                        Statuses.Clear();
                    }
                    // do not change loading flag in inner method
                    await ReadMore(null, false).ConfigureAwait(false);
                }
                finally
                {
                    if (complete)
                    {
                        IsLoading = false;
                    }
                }
            });
        }

        #endregion

        #region Focusing

        public event Action FocusRequired;

        public void RequestFocus()
        {
            FocusRequired.SafeInvoke();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Pre-invalidate timeline
        /// </summary>
        /// <returns>if returns false, keep loading yet</returns>
        protected abstract bool PreInvalidateTimeline();

        private bool CheckAcceptStatus(TwitterStatus status)
        {
            return !MuteBlockManager.IsUnwanted(status) && CheckAcceptStatusCore(status);
        }

        protected abstract bool CheckAcceptStatusCore(TwitterStatus status);

        protected abstract IObservable<TwitterStatus> Fetch(long? maxId, int? count);

        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        ~TimelineModelBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            IsSubscribeBroadcaster = false;
            lock (_statusIdCache)
            {
                _statusIdCache.Clear();
            }
            Statuses.Clear();
        }

        protected enum TimelineModelState
        {
            Activated,
            Deactivated,
            Disposed,
        }
    }
}
