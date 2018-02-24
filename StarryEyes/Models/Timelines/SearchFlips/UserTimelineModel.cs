using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Settings;

namespace StarryEyes.Models.Timelines.SearchFlips
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
            return Observable.Defer(() =>
            {
                switch (_type)
                {
                    case TimelineType.User:
                        return account.CreateAccessor()
                                      .GetUserTimelineAsync(new UserParameter(_userId), count,
                                          null, maxId, false, true, CancellationToken.None)
                                      .ToObservable();
                    case TimelineType.Favorites:
                        return account.CreateAccessor()
                                      .GetFavoritesAsync(new UserParameter(_userId), count,
                                          null, maxId, CancellationToken.None)
                                      .ToObservable();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }).SelectMany(s => s.Result);
        }
    }

    public enum TimelineType
    {
        User,
        Favorites
    }
}