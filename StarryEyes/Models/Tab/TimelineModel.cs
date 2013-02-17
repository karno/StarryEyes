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
        public static readonly int TimelineChunkCount = 120;
        public static readonly int TimelineChunkCountBounce = 30;

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

            // add handler
            _disposable.Add(StatusStore.StatusPublisher
                                       .Where(sn => sn.IsAdded && evaluator(sn.Status))
                                       .Select(s => s.Status)
                                       .Subscribe(AddStatus));
            // remove handler
            _disposable.Add(StatusStore.StatusPublisher
                                       .Where(sn => !sn.IsAdded || !evaluator(sn.Status))
                                       .Select(s => s.StatusId)
                                       .Subscribe(RemoveStatus));
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
                if (_isSuppressTimelineTrimming != value)
                {
                    _isSuppressTimelineTrimming = value;
                    if (!value)
                        TrimTimeline();
                }
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
            _statusIdCache.Clear();
            _statuses.Clear();
        }

        public event Action OnNewStatusArrived;

        private void AddStatus(TwitterStatus status)
        {
            bool add;
            lock (_sicLocker)
            {
                add = _statusIdCache.AddDistinct(status.Id);
            }
            if (add)
            {
                // estimate point
                if (!_isSuppressTimelineTrimming)
                {
                    var addpoint = _statuses.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count();
                    if (addpoint > TimelineChunkCount)
                    {
                        _statusIdCache.Remove(status.Id);
                        return;
                    }
                }
                _statuses.Insert(
                    i => i.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count(),
                    status);
                if (_statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce &&
                    _trimCount == 0)
                    Task.Run(() => TrimTimeline());
                Action handler = OnNewStatusArrived;
                if (handler != null)
                    handler();
            }
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
            if (Interlocked.Exchange(ref _trimCount, 1) != 0) return;
            System.Diagnostics.Debug.WriteLine("trimming...");
            try
            {
                DateTime lastCreatedAt = _statuses[TimelineChunkCount].CreatedAt;
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
                System.Diagnostics.Debug.WriteLine("trim executed. reflect it... (" + removedIds.Count + ")");
                lock (_sicLocker)
                {
                    removedIds.ForEach(i => _statusIdCache.Remove(i));
                    System.Diagnostics.Debug.WriteLine("after: statuses: " + _statuses.Count + " / sic: " + _statusIdCache.Count);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _trimCount, 0);
            }
        }
    }
}