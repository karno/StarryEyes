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

        class StatusStoreInternal : StoreBase<TwitterStatus, Status>
        {
            protected override long KeyProvider(TwitterStatus item)
            {
                return item.Id;
            }

            protected override void WriteItem(TwitterStatus item)
            {
                Database.Status.AddObject(item.ToDbStatus());
            }

            protected override TwitterStatus FindItem(long id)
            {
                return Database.Status.Where(s => s.Id == id)
                    .Take(1)
                    .Select(s => s.ToTwitterStatus())
                    .FirstOrDefault();
            }

            protected override IEnumerable<TwitterStatus> FindItems(Expression<Func<Status, bool>> predicate)
            {
                throw new NotImplementedException();
            }

            protected override void DeleteItem(long key)
            {
                throw new NotImplementedException();
            }
        }

        private static StatusStoreInternal store = new StatusStoreInternal();

        public static void Store(TwitterStatus status)
        {
            throw new NotImplementedException();
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
