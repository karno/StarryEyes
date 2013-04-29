using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Tab
{
    public class TimelineModel : IDisposable
    {
        public static readonly int TimelineChunkCount = 256;
        public static readonly int TimelineChunkCountBounce = 64;

        private readonly CompositeDisposable _disposable;
        private readonly Func<long?, int, bool, IObservable<TwitterStatus>> _fetcher;

        private readonly object _sicLocker = new object();
        private readonly AVLTree<long> _statusIdCache;

        private readonly ObservableSynchronizedCollectionEx<TwitterStatus> _statuses
            = new ObservableSynchronizedCollectionEx<TwitterStatus>();

        private bool _isSuppressTimelineTrimming;

        public TimelineModel(Func<TwitterStatus, bool> evaluator,
                             Func<long?, int, bool, IObservable<TwitterStatus>> fetcher)
        {
            _fetcher = fetcher;
            _statusIdCache = new AVLTree<long>();
            _disposable = new CompositeDisposable();

            // register handler
            _disposable.Add(StatusStore.StatusPublisher
                .Subscribe(sn =>
                {
                    if (sn.IsAdded && evaluator(sn.Status))
                    {
                        AddStatus(sn.Status);
                    }
                    else
                    {
                        RemoveStatus(sn.StatusId);
                    }
                }));
        }

        public ObservableSynchronizedCollectionEx<TwitterStatus> Statuses
        {
            get { return _statuses; }
        }

        public bool IsSuppressTimelineTrimming
        {
            get { return _isSuppressTimelineTrimming; }
            set
            {
                if (this._isSuppressTimelineTrimming == value) return;
                this._isSuppressTimelineTrimming = value;
                if (!value)
                {
                    this.TrimTimeline();
                }
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            _statusIdCache.Clear();
            _statuses.Clear();
        }

        public event Action<TwitterStatus> NewStatusArrival;

        private void AddStatus(TwitterStatus status)
        {
            bool add;
            lock (_sicLocker)
            {
                add = _statusIdCache.AddDistinct(status.Id);
            }
            if (!add) return;
            // estimate point
            if (!this._isSuppressTimelineTrimming)
            {
                var addpoint = this._statuses.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count();
                if (addpoint > TimelineChunkCount)
                {
                    lock (this._sicLocker)
                    {
                        this._statusIdCache.Remove(status.Id);
                    }
                    return;
                }
            }
            this._statuses.Insert(
                i => i.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count(),
                status);
            if (this._statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                Interlocked.Exchange(ref this._trimCount, 1) == 0)
            {
                Task.Run(() => this.TrimTimeline());
            }
            var handler = this.NewStatusArrival;
            if (handler != null)
                handler(status);
        }

        private void RemoveStatus(long id)
        {
            bool remove;
            lock (_sicLocker)
            {
                remove = _statusIdCache.Remove(id);
            }
            if (remove)
            {
                // remove
                _statuses.RemoveWhere(s => s.Id == id);
            }
        }

        public IObservable<Unit> ReadMore(long? maxId, bool batch = false)
        {
            return Observable.Defer(() => _fetcher(maxId, TimelineChunkCount, batch))
                             .Do(AddStatus)
                             .OfType<Unit>();
        }

        private int _trimCount;
        private void TrimTimeline()
        {
            if (_isSuppressTimelineTrimming) return;
            if (_statuses.Count <= TimelineChunkCount) return;
            try
            {
                var lastCreatedAt = _statuses[TimelineChunkCount].CreatedAt;
                var removedIds = new List<long>();
                _statuses.RemoveWhere(t =>
                {
                    if (t.CreatedAt < lastCreatedAt)
                    {
                        removedIds.Add(t.Id);
                        return true;
                    }
                    return false;
                });
                lock (_sicLocker)
                {
                    removedIds.ForEach(i => _statusIdCache.Remove(i));
                }
            }
            finally
            {
                Interlocked.Exchange(ref _trimCount, 0);
            }
        }
    }
}
