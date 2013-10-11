using System;
using System.Linq;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timeline.SearchFlips
{
    public class UserTimelineModel : TimelineModelBase
    {
        private readonly long _userId;
        private readonly TimelineType _type;

        public UserTimelineModel(long userId, TimelineType type)
        {
            _userId = userId;
            _type = type;
        }

        protected override void PreInvalidateTimeline()
        {
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            // only from web-fetching
            return true;
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            var account =
                Setting.Accounts.Collection.FirstOrDefault(
                    a => a.RelationData.IsFollowing(_userId)) ??
                Setting.Accounts.GetRandomOne();
            switch (_type)
            {
                case TimelineType.User:
                    return account.GetUserTimelineAsync(_userId, count, maxId: maxId).ToObservable();
                case TimelineType.Favorites:
                    return account.GetFavoritesAsync(_userId, count, maxId: maxId).ToObservable();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum TimelineType
    {
        User,
        Favorites
    }
}
