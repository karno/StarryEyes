using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Hubs;
using StarryEyes.Models.Stores.Internal;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Storage for twitter statuses.
    /// </summary>
    public static class StatusStore
    {
        #region publish block

        private static readonly Subject<StatusNotification> _statusPublisher = new Subject<StatusNotification>();

        public static IObservable<StatusNotification> StatusPublisher
        {
            get { return _statusPublisher; }
        }

        #endregion

        private static volatile bool _isInShutdown = false;

        private static DataStoreBase<long, TwitterStatus> _store;

        public static void Initialize()
        {
            // initialize
            if (StoreOnMemoryObjectPersistence.IsPersistentDataExisted("statuses"))
            {
                _store = new PersistentDataStore<long, TwitterStatus>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "statuses"), new IdReverseComparer(),
                    manageData: StoreOnMemoryObjectPersistence.GetPersistentData("statuses"));
            }
            else
            {
                _store = new PersistentDataStore<long, TwitterStatus>
                    (_ => _.Id, Path.Combine(App.DataStorePath, "statuses"), new IdReverseComparer());
            }
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
                _statusPublisher.OnNext(new StatusNotification()
                {
                    IsAdded = true,
                    Status = status,
                    StatusId = status.Id
                });
            }
            _store.Store(status);
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
            return _store.Get(id)
                .Do(_ => Store(_, false)); // add to local cache
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
                .Distinct(_ => _.Id)
                .OrderByDescending(_ => _.Id)
                .Take(count.Value);
        }

        /// <summary>
        /// Remove tweet from store.
        /// </summary>
        /// <param name="id">removing tweet's id</param>
        /// <param name="publish">publish removing notification to children</param>
        public static async void Remove(long id, bool publish = true)
        {
            if (_isInShutdown) return;
            if (publish)
                _statusPublisher.OnNext(new StatusNotification() { IsAdded = false, StatusId = id });
            var removal = await Get(id);
            if (removal == null) return;
            _store.Remove(id);
            if (publish)
                _statusPublisher.OnNext(new StatusNotification() { IsAdded = false, Status = removal, StatusId = id });
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
