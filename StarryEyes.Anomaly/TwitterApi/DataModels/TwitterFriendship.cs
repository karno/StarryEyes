using System;

namespace StarryEyes.Anomaly.TwitterApi.DataModels
{
    public class TwitterFriendship
    {
        public TwitterFriendship() { }

        public TwitterFriendship(dynamic json)
        {
            var rel = json.relationship;
            var src = rel.source;
            var tgt = rel.target;
            SourceId = Int64.Parse(src.id_str);
            SourceScreenName = src.screen_name;
            TargetId = Int64.Parse(tgt.id_str);
            TargetScreenName = tgt.screen_name;
            IsSourceFollowingTarget = src.following;
            IsTargetFollowingSource = src.followed_by;
            IsBlocking = src.blocking;
            IsMuting = src.muting;
            // if source is not following target, twitter always returns false.
            IsWantRetweets = IsSourceFollowingTarget ? ((bool?)src.want_retweets) : null;
        }

        public long SourceId { get; set; }

        public string SourceScreenName { get; set; }

        public long TargetId { get; set; }

        public string TargetScreenName { get; set; }

        public bool IsSourceFollowingTarget { get; set; }

        public bool IsTargetFollowingSource { get; set; }

        public bool IsBlocking { get; set; }

        public bool? IsMuting { get; set; }

        public bool? IsWantRetweets { get; set; }
    }
}
