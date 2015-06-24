using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timelines.SearchFlips
{
    public class UserTimelineModel : TimelineModelBase
    {
        private readonly long _userId;
        private readonly TimelineType _type;

        public UserTimelineModel(long userId, TimelineType type)
        {
            this._userId = userId;
            this._type = type;
        }

        protected override bool PreInvalidateTimeline()
        {
            return true;
        }

        protected override bool CheckAcceptStatusCore(TwitterStatus status)
        {
            // only from web-fetching
            return true;
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            var account = Setting.Accounts.GetRelatedOne(_userId);
            switch (_type)
            {
                case TimelineType.User:
                    return account.GetUserTimelineAsync(ApiAccessProperties.Default, new UserParameter(_userId), count,
                            maxId: maxId, includeRetweets: true).ToObservable().SelectMany(s => s.Result);
                case TimelineType.Favorites:
                    return account.GetFavoritesAsync(ApiAccessProperties.Default, new UserParameter(_userId), count,
                            maxId: maxId).ToObservable().SelectMany(s => s.Result);
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
