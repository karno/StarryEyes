using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Settings;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Find local cache, if not existed, query to Twitter.
    /// </summary>
    public static class StoreHelper
    {
        [Obsolete]
        public static IObservable<TwitterStatus> MergeStore(TwitterStatus status)
        {
            StatusStore.Store(status);
            return Observable.Return(status);
            /*
                return StatusStore.Get(status.Id)
                                  .ConcatIfEmpty(() =>
                                  {
                                      StatusStore.Store(status);
                                      return Observable.Return(status);
                                  });
            */
        }

        [Obsolete]
        public static IObservable<TwitterStatus> NotifyAndMergeStore(TwitterStatus status)
        {
            StatusStore.Store(status);
            return Observable.Return(status);
            /*
            return StatusStore.Get(status.Id)
                              .ConcatIfEmpty(() =>
                              {
                                  StatusStore.Store(status);
                                  NotificationModel.NotifyNewArrival(status);
                                  return Observable.Return(status);
                              });
	    */
        }

        public static IObservable<TwitterStatus> GetTweet(long id)
        {
            return StatusStore.Get(id)
                              .Where(_ => _ != null)
                              .ConcatIfEmpty(() => Setting.Accounts.GetRandomOne().ShowTweet(id).ToObservable()
                                                          .Do(s => StatusStore.Store(s, false)));
        }

        public static IObservable<TwitterUser> GetUser(long id)
        {
            return UserStore.Get(id)
                            .Where(_ => _ != null)
                            .ConcatIfEmpty(() => Setting.Accounts.GetRandomOne().ShowUser(id).ToObservable()
                                                        .Do(UserStore.Store));
        }

        public static IObservable<TwitterUser> GetUser(string screenName)
        {
            return UserStore.Get(screenName)
                            .Where(_ => _ != null)
                            .ConcatIfEmpty(() => Setting.Accounts.GetRandomOne().ShowUser(screenName).ToObservable()
                                                        .Do(UserStore.Store));
        }
    }
}
