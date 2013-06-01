using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterFriendship
    {
        public TwitterFriendship() { }

        public TwitterFriendship(dynamic json)
        {
            SourceId = Int64.Parse(json.source.id_str);
            SourceScreenName = json.source.screen_name;
            TargetId = Int64.Parse(json.target.id_str);
            TargetScreenName = json.target.screen_name;
            IsSourceFollowingTarget = json.source.following;
            IsTargetFollowingSource = json.source.followed_by;
            IsBlocking = json.source.blocking;
        }

        public long SourceId { get; set; }

        public string SourceScreenName { get; set; }

        public long TargetId { get; set; }

        public string TargetScreenName { get; set; }

        public bool IsSourceFollowingTarget { get; set; }

        public bool IsTargetFollowingSource { get; set; }

        public bool IsBlocking { get; set; }
    }
}
