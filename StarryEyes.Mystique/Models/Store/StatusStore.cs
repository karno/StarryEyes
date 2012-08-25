using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Persistent;
using System.Threading;

namespace StarryEyes.Mystique.Models.Store
{
    /// <summary>
    /// Storage for twitter statuses.
    /// </summary>
    public static class StatusStore
    {
        #region publish block

        private static Subject<StatusNotification> statusPublisher = new Subject<StatusNotification>();

        public static IObservable<StatusNotification> StatusPublisher
        {
            get { return statusPublisher; }
        }

        #endregion

        private static DataStoreBase<long, TwitterStatus> store;

        static StatusStore()
        {
            // initialize
            store = new PersistentDataStore<long, TwitterStatus>
                (_ => _.Id, Path.Combine(App.DataStorePath, "statuses"));
            App.OnApplicationFinalize += Shutdown;
        }

        /// <summary>
        /// Get stored status counts.<para />
        /// If you want this param, please consider using StatisticsHub instead of this.
        /// </summary>
        public static int Count
        {
            get { return store.Count; }
        }

        /// <summary>
        /// Store a tweet.
        /// </summary>
        /// <param name="status">storing status</param>
        /// <param name="publish">flag of publish status for other listening children</param>
        public static void Store(TwitterStatus status, bool publish = true)
        {
            if (publish)
                statusPublisher.OnNext(new StatusNotification()
                {
                    IsAdded = true,
                    Status = status,
                    StatusId = status.Id
                });
            store.Store(status);
            UserStore.Store(status.User);
        }

        /// <summary>
        /// Get tweet.
        /// </summary>
        /// <param name="id">find id</param>
        /// <returns>contains a tweet or empty observable.</returns>
        public static IObservable<TwitterStatus> Get(long id)
        {
            return store.Get(id)
                .Do(_ => Store(_, false)); // add to local cache
        }

        /// <summary>
        /// Find tweets.
        /// </summary>
        /// <param name="predicate">find predicate</param>
        /// <param name="range">finding range</param>
        /// <returns>results observable sequence.</returns>
        public static IObservable<TwitterStatus> Find(Func<TwitterStatus, bool> predicate, FindRange<long> range = null, int? count = null)
        {
            return store.Find(predicate, range, count);
        }

        /// <summary>
        /// Remove tweet from store.
        /// </summary>
        /// <param name="id">removing tweet's id</param>
        /// <param name="publish">publish removing notification to children</param>
        public static void Remove(long id, bool publish = true)
        {
            if (publish)
                statusPublisher.OnNext(new StatusNotification() { IsAdded = false, StatusId = id });
            store.Remove(id);
        }

        /// <summary>
        /// Shutdown store.
        /// </summary>
        internal static void Shutdown()
        {
            store.Dispose();
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
