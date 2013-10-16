using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Settings;

namespace StarryEyes.Models.Stores
{
    /// <summary>
    /// Find local cache, if not existed, query to Twitter.
    /// </summary>
    public static class StoreHelper
    {
        public static IObservable<TwitterStatus> GetTweet(long id)
        {
            return StatusProxy.GetStatusAsync(id)
                              .ToObservable()
                              .Where(_ => _ != null)
                              .ConcatIfEmpty(() =>
                                             Setting.Accounts.GetRandomOne().ShowTweetAsync(id).ToObservable()
                                                    .Do(StatusInbox.Queue));
        }

        public static IObservable<TwitterUser> GetUser(long id)
        {
            return UserProxy.GetUserAsync(id)
                            .ToObservable()
                            .Where(_ => _ != null)
                            .ConcatIfEmpty(() =>
                                           Setting.Accounts.GetRandomOne().ShowUserAsync(id).ToObservable()
                                                  .Do(u => Task.Run(() => UserProxy.StoreUserAsync(u))));
        }

        public static IObservable<TwitterUser> GetUser(string screenName)
        {
            return UserProxy.GetUserAsync(screenName)
                            .ToObservable()
                            .Where(_ => _ != null)
                            .ConcatIfEmpty(
                                () => Setting.Accounts.GetRandomOne().ShowUserAsync(screenName).ToObservable()
                                             .Do(u => Task.Run(() => UserProxy.StoreUserAsync(u))));
        }
    }
}
