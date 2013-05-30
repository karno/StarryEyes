using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamStatusActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public string Event { get; set; }

        public TwitterStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
