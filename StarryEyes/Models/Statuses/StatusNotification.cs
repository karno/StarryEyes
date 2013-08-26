using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Models.Statuses
{
    public class StatusNotification
    {
        public StatusNotification(long id)
        {
            this.Status = null;
            this.StatusId = id;
            this.IsAdded = true;
        }

        public StatusNotification(TwitterStatus status)
        {
            this.Status = status;
            this.StatusId = status.Id;
            this.IsAdded = true;
        }
        public bool IsAdded { get; private set; }
        public long StatusId { get; private set; }
        public TwitterStatus Status { get; private set; }
    }
}