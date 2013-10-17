using System;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;

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

        public bool IsNew { get; private set; }

        public bool IsAdded { get; private set; }

        public long StatusId { get; private set; }

        public TwitterStatus Status { get; private set; }
    }
}