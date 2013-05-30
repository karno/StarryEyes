using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels.StreamModels
{
    public class StreamListActivity
    {
        public TwitterUser Target { get; set; }

        public TwitterUser Source { get; set; }

        public string Event { get; set; }

        public TwitterList List { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
