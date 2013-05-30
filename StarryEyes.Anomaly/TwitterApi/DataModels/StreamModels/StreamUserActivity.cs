using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamUserActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public string Event { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
