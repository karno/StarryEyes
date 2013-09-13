namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    /// <summary>
    /// Represents favorite/retweet activity info.
    /// </summary>
    public class TwitterActivity
    {
        public bool IsNewRecord { get { return DatabaseId == null; } }

        public long? DatabaseId { get; set; }

        public long UserId { get; set; }
    }
}
