using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Albireo.Data;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Tab
{
    public class TimelineModel : IDisposable
    {
        public readonly int TimelineChunkCount = 120;
        public readonly int TimelineChunkCountBounce = 30;
        private readonly CompositeDisposable _disposable;
        private readonly Func<long?, int, IObservable<TwitterStatus>> _fetcher;

        private readonly object _sicLocker = new object();
        private readonly AVLTree<long> _statusIdCache;

        private readonly ObservableSynchronizedCollectionEx<TwitterStatus> _statuses
            = new ObservableSynchronizedCollectionEx<TwitterStatus>();

        private bool _isSuppressTimelineTrimming;

        public TimelineModel(Func<TwitterStatus, bool> evaluator,
                             Func<long?, int, IObservable<TwitterStatus>> fetcher)
        {
            _fetcher = fetcher;
            _statusIdCache = new AVLTree<long>();
            _disposable = new CompositeDisposable();

            // listen status stream
            _disposable.Add(StatusStore.StatusPublisher
                                       .Where(sn => !sn.IsAdded || evaluator(sn.Status))
                                       .Subscribe(AcceptStatusNotification));
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

        private void AcceptStatusNotification(StatusNotification notification)
        {
            if (notification.IsAdded)
                AddStatus(notification.Status);
            else
                RemoveStatus(notification.StatusId);
        }

        private void AddStatus(TwitterStatus status)
        {
            bool add = false;
            lock (_sicLocker)
            {
                add = _statusIdCache.AddDistinct(status.Id);
            }
            if (add)
            {
                // add
                _statuses.Insert(
                    i => i.TakeWhile(_ => _.CreatedAt > status.CreatedAt).Count(),
                    status);
                if (_statusIdCache.Count > TimelineChunkCount + TimelineChunkCountBounce)
                    TrimTimeline();
                Action handler = OnNewStatusArrived;
                if (handler != null)
                    handler();
            }
        }

        private void RemoveStatus(long id)
        {
            bool remove = false;
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

        public IObservable<Unit> ReadMore(long? maxId)
        {
            return Observable.Defer(() => _fetcher(maxId, TimelineChunkCount))
                             .Do(AddStatus)
                             .OfType<Unit>();
        }

        private void TrimTimeline()
        {
            if (_isSuppressTimelineTrimming) return;
            if (_statuses.Count < TimelineChunkCount + TimelineChunkCountBounce) return;
            DateTime lastCreatedAt = _statuses[TimelineChunkCount].CreatedAt;
            var removedIds = new List<long>();
            _statuses.RemoveWhere(t =>
            {
                if (t.CreatedAt < lastCreatedAt)
                {
                    removedIds.Add(t.Id);
                    return true;
                }
                else
                {
                    return false;
                }
            });
            lock (_sicLocker)
            {
                removedIds.ForEach(i => _statusIdCache.Remove(i));
            }
        }
    }
}