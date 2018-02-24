using System;
using System.Threading.Tasks;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Receiving.Handling
{
    public class StatusNotification
    {
        public StatusNotification(long id)
        {
            Status = null;
            StatusId = id;
            IsAdded = false;
            IsNew = false;
        }

        public StatusNotification([CanBeNull] TwitterStatus status, bool added = true, bool isNew = true)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
            StatusId = status.Id;
            IsAdded = added;
            IsNew = isNew;
        }

        /// <summary>
        /// Notify this status as new tweet
        /// </summary>
        public bool IsNew { get; }

        /// <summary>
        /// Flag for determine new receive notification or deleted notification
        /// </summary>
        public bool IsAdded { get; }

        /// <summary>
        /// Target status id
        /// </summary>
        public long StatusId { get; }

        /// <summary>
        /// Target status
        /// </summary>
        [CanBeNull]
        public TwitterStatus Status { get; }
    }

    public class StatusModelNotification
    {
        public static async Task<StatusModelNotification> FromStatusNotification(StatusNotification notification,
            bool isNew)
        {
            var model = notification.Status == null
                ? null
                : await StatusModel.Get(notification.Status).ConfigureAwait(false);
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
            IsNew = isNew;
            IsAdded = isAdded;
            StatusId = statusId;
            StatusModel = model;
        }

        public bool IsNew { get; }

        public bool IsAdded { get; }

        public long StatusId { get; }

        [CanBeNull]
        public StatusModel StatusModel { get; }
    }
}