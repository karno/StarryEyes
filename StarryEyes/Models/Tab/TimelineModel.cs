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

        private readonly ObservableSynchronizedCollectionEx<StatusModel> _statuses
            = new ObservableSynchronizedCollectionEx<StatusModel>();

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

        public ObservableSynchronizedCollectionEx<StatusModel> Statuses
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

        private async void AddStatus(TwitterStatus status)
        {
            bool add;
            lock (_sicLocker)
            {
                add = _statusIdCache.AddDistinct(status.Id);
            }
            if (!add) return;
            // estimate point
            var model = await StatusModel.Get(status);
            var stamp = model.Status.CreatedAt;
            if (!this._isSuppressTimelineTrimming)
            {
                StatusModel lastModel;
                if (this.Statuses.TryIndexOf(TimelineChunkCount, out lastModel) &&
                    lastModel != null &&
                    lastModel.Status.CreatedAt > stamp)
                {
                    lock (this._sicLocker)
                    {
                        this._statusIdCache.Remove(model.Status.Id);
                    }
                    return;
                }
            }
            this._statuses.Insert(
                i => i.TakeWhile(s => s.Status.CreatedAt > stamp).Count(),
                model);
            if (this._statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                Interlocked.Exchange(ref this._trimCount, 1) == 0)
            {
#pragma warning disable 4014
                Task.Run(() => this.TrimTimeline());
#pragma warning restore 4014
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
                _statuses.RemoveWhere(s => s.Status.Id == id);
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
