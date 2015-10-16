using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Receiving.Handling
{
    public class StatusNotification
    {
        public StatusNotification(long id)
        {
            this.Status = null;
            this.StatusId = id;
            this.IsAdded = false;
            this.IsNew = false;
        }

        public StatusNotification([NotNull] TwitterStatus status, bool added = true, bool isNew = true)
        {
            if (status == null) throw new ArgumentNullException("status");
            this.Status = status;
            this.StatusId = status.Id;
            this.IsAdded = added;
            this.IsNew = isNew;
        }

        /// <summary>
        /// Notify this status as new tweet
        /// </summary>
        public bool IsNew { get; private set; }

        /// <summary>
        /// Flag for determine new receive notification or deleted notification
        /// </summary>
        public bool IsAdded { get; private set; }

        /// <summary>
        /// Target status id
        /// </summary>
        public long StatusId { get; private set; }

        /// <summary>
        /// Target status
        /// </summary>
        [CanBeNull]
        public TwitterStatus Status { get; private set; }
    }

    public class StatusModelNotification
    {
        public static async Task<StatusModelNotification> FromStatusNotification(StatusNotification notification, bool isNew)
        {
            var model = notification.Status == null ? null : await StatusModel.Get(notification.Status).ConfigureAwait(false);
            return new StatusModelNotification(model, notification.IsAdded, isNew, notification.StatusId);
        }

        public StatusModelNotification(long id)
            : this(null, false, false, id)
        {
        }

        public StatusModelNotification([NotNull] StatusModel model, bool isAdded = true, bool isNew = true)
            : this(model, isAdded, isNew, model.Status.Id)
        {
        }

        private StatusModelNotification(StatusModel model, bool isAdded, bool isNew, long statusId)
        {
            this.IsNew = isNew;
            this.IsAdded = isAdded;
            this.StatusId = statusId;
            this.StatusModel = model;
        }

        public bool IsNew { get; private set; }

        public bool IsAdded { get; private set; }

        public long StatusId { get; private set; }

        [CanBeNull]
        public StatusModel StatusModel { get; private set; }
    }
}