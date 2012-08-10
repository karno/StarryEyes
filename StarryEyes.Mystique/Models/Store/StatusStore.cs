using System;
using System.Reactive.Subjects;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Vanille.DataStore;
using StarryEyes.Vanille.DataStore.Simple;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Linq.Expressions;
using System.Linq;
using StarryEyes.Vanille.DataStore.Persistent;
using System.IO;

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

        private static DataStoreBase<long, TwitterStatus> store = new PersistentDataStore<long, TwitterStatus>
            (_ => _.Id,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Krile", "StarryEyes", "store"));

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
        }

        public static IObservable<TwitterStatus> Get(long id)
        {
            return store.Get(id);
        }

        public static IObservable<TwitterStatus> Find(Func<TwitterStatus, bool> predicate)
        {
            return store.Find(predicate);
        }

        public static void Remove(long id)
        {
            store.Remove(id);
        }

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
