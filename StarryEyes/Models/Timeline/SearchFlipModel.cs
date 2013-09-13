using System;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Models.Timeline
{
    public class SearchFlipModel : TimelineModelBase
    {
        private readonly long _userId;

        public SearchFlipModel(long userId)
        {
            _userId = userId;
        }

        protected override void InvalidateCache()
        {
            throw new NotImplementedException();
        }

        protected override bool CheckAcceptStatus(TwitterStatus status)
        {
            throw new NotImplementedException();
        }

        protected override IObservable<TwitterStatus> Fetch(long? maxId, int? count)
        {
            throw new NotImplementedException();
        }
    }

    public enum TimelineType
    {
        User,
        Favorites
    }
}
