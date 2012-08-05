using System;
using System.Reactive.Subjects;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Simple;

namespace StarryEyes.Mystique.Models.Store
{
    /// <summary>
    /// Storage for twitter statuses.
    /// </summary>
    public static class StatusStore
    {
        /// <summary>
        /// Core data store.
        /// </summary>
        private static DataStoreBase<long, TwitterStatus> store;

        #region publish block 

        private static Subject<StatusNotification> statusPublisher = new Subject<StatusNotification>();

        public static IObservable<StatusNotification> StatusPublisher
        {
            get { return statusPublisher; }
        }

        #endregion

        static StatusStore()
        {
            store = new SimpleDataStore<long, TwitterStatus>(status => status.Id);
        }

        /// <summary>
        /// Store the status.
        /// </summary>
        /// <param name="status">storing status</param>
        /// <param name="publish">flag of publish to subscribing children</param>
        public static void Store(TwitterStatus status, bool publish = true)
        {
            if (publish)
                statusPublisher.OnNext(new StatusNotification()
                {
                    IsAdded = true,
                    StatusId = status.Id,
                    Status = status
                });
            store.Store(status);
        }

        /// <summary>
        /// Get a status.
        /// </summary>
        /// <param name="id">status id</param>
        /// <returns>a status (or empty)</returns>
        public static IObservable<TwitterStatus> Get(long id)
        {
            return store.Get(id);
        }

        /// <summary>
        /// Find statuses.
        /// </summary>
        /// <param name="predicate">predicate for finding status</param>
        /// <returns>statuses</returns>
        public static IObservable<TwitterStatus> Find(Func<TwitterStatus, bool> predicate)
        {
            return store.Find(predicate);
        }

        /// <summary>
        /// Remove status.
        /// </summary>
        /// <param name="id"></param>
        public static void Remove(long id, bool publish = true)
        {
            if (publish)
                statusPublisher.OnNext(new StatusNotification()
                {
                    IsAdded = false,
                    StatusId = id
                });
            store.Remove(id);
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
