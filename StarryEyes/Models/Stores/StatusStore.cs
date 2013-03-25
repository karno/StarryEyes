using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores.Internal;
using StarryEyes.Models.Subsystems;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Storage for twitter statuses.
    /// </summary>
    public static class StatusStore
    {
        private const int ChunkCount = 4;

        #region publish block

        private static readonly Subject<StatusNotification> _statusPublisher =
            new Subject<StatusNotification>();

        public static IObservable<StatusNotification> StatusPublisher
        {
            get { return _statusPublisher; }
        }

        #endregion

        private static volatile bool _isInShutdown;

        private static DataStoreBase<long, TwitterStatus> _store;

        private static DateTime _dispatch = DateTime.Now;

        private static SingleThreadDispatcher<TwitterStatus> _dispatcher;

        public static void Initialize()
        {
            // initialize
            if (StoreOnMemoryObjectPersistence.IsPersistentDataExisted("statuses"))
            {
                _store = new PersistentDataStore<long, TwitterStatus>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "statuses"), new IdReverseComparer(),
                    manageData: StoreOnMemoryObjectPersistence.GetPersistentData("statuses"), chunkCount: ChunkCount);
            }
            else
            {
                _store = new PersistentDataStore<long, TwitterStatus>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "statuses"), new IdReverseComparer(), ChunkCount);
            }
            _dispatcher = new SingleThreadDispatcher<TwitterStatus>(_store.Store);
            App.OnApplicationFinalize += Shutdown;
        }

        /// <summary>
        /// Get stored status counts.<para />
        /// If you want this param, please consider using StatisticsHub instead of this.
        /// </summary>
        public static int Count
        {
            get { return _store.Count; }
        }

        /// <summary>
        /// Store a tweet.
        /// </summary>
        /// <param name="status">storing status</param>
        /// <param name="publish">flag of publish status for other listening children</param>
        public static void Store(TwitterStatus status, bool publish = true)
        {
            if (_isInShutdown) return;
            if (publish)
            {
                if (!StatisticsService.TooFastWarning ||
                    (DateTime.Now - _dispatch).TotalSeconds > 1)
                {
                    _dispatch = DateTime.Now;
                    Task.Run(() => _statusPublisher.OnNext(new StatusNotification(status, true)));
                }
            }
            _dispatcher.Send(status);
            UserStore.Store(status.User);
        }

        /// <summary>
        /// Get tweet.
        /// </summary>
        /// <param name="id">find id</param>
        /// <returns>contains a tweet or empty observable.</returns>
        public static IObservable<TwitterStatus> Get(long id)
        {
            if (_isInShutdown) return Observable.Empty<TwitterStatus>();
            return _store.Get(id);
        }

        /// <summary>
        /// Find tweets.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <param name="range">finding range</param>
        /// <param name="count">count of findings</param>
        /// <returns>results observable sequence.</returns>
        public static IObservable<TwitterStatus> Find(Func<TwitterStatus, bool> predicate,
            FindRange<long> range = null, int? count = null)
        {
            if (_isInShutdown) return Observable.Empty<TwitterStatus>();
            var result = _store.Find(predicate, range, count);
            if (count == null)
                return result;
            return result
                .Distinct(_ => _.Id);
        }

        private const int BatchWaitSec = 5;
        private static readonly object _batchLock = new object();
        private static List<Func<TwitterStatus, bool>> _predicates;
        private static Subject<TwitterStatus> _batchResult;

        /// <summary>
        /// Find tweets with batch query.
        /// </summary>
        /// <param name="predicate">finding predicate.</param>
        /// <param name="count">find status count</param>
        /// <returns></returns>
        public static IObservable<TwitterStatus> FindBatch(Func<TwitterStatus, bool> predicate, int count)
        {
            Subject<TwitterStatus> batch;
            bool register = false;
            lock (_batchLock)
            {
                if (_batchResult == null)
                {
                    _batchResult = new Subject<TwitterStatus>();
                    _predicates = new List<Func<TwitterStatus, bool>>();
                    register = true;
                }
                batch = _batchResult;
                _predicates.Add(predicate);
            }
            if (register)
            {
                // queue batch
                Observable.Timer(TimeSpan.FromSeconds(BatchWaitSec))
                          .Subscribe(_ =>
                          {
                              Subject<TwitterStatus> callback;
                              Func<TwitterStatus, bool> find;
                              lock (_batchLock)
                              {
                                  var pa = _predicates.ToArray();
                                  find = t => pa.Any(p => p(t));
                                  callback = _batchResult;
                                  _predicates = null;
                                  _batchResult = null;
                              }
                              Find(find)
                                  .Distinct(s => s.Id)
                                  .Subscribe(callback);
                          });
            }
            return batch
                .Where(predicate)
                .Take(count);
        }

        /// <summary>
        /// Remove tweet from store.
        /// </summary>
        /// <param name="id">removing tweet's id</param>
        /// <param name="publish">publish removing notification to children</param>
        public static void Remove(long id, bool publish = true)
        {
            if (_isInShutdown) return;
            if (publish)
                _statusPublisher.OnNext(new StatusNotification(id, false));
            Get(id)
                .Subscribe(removal =>
                {
                    if (removal == null) return;
                    _store.Remove(id);
                    if (publish)
                        _statusPublisher.OnNext(new StatusNotification(removal, false));
                });
        }

        /// <summary>
        /// Shutdown store.
        /// </summary>
        internal static void Shutdown()
        {
            _isInShutdown = true;
            if (_store != null)
            {
                _store.Dispose();
                var pds = (PersistentDataStore<long, TwitterStatus>)_store;
                StoreOnMemoryObjectPersistence.MakePersistent("statuses", pds.GetManageDatas());
            }
        }
    }

    public class StatusNotification
    {
        public StatusNotification(TwitterStatus status, bool isAdded)
        {
            Debug.Assert(status != null, "status could not be null.");
            this.Status = status;
            this.StatusId = status.Id;
            this.IsAdded = isAdded;
        }

        public StatusNotification(long id, bool isAdded)
        {
            if (isAdded)
                throw new ArgumentException("isAdded could not be true in this overload.");
            this.StatusId = id;
            this.IsAdded = false;
        }

        /// <summary>
        /// flag of added status or removed
        /// </summary>
        public bool IsAdded { get; set; }

        /// <summary>
        /// status id.
        /// </summary>
        public long StatusId { get; set; }

        /// <summary>
        /// actual status.<para />
        /// this property is available when this notification notifys status is added.
        /// </summary>
        public TwitterStatus Status { get; set; }
    }
}
