using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Hubs
{
    /// <summary>
    /// Find local cache, if not existed, query to Twitter.
    /// </summary>
    public static class StoreHub
    {
        public static IObservable<TwitterStatus> MergeStore(TwitterStatus status)
        {
            return StatusStore.Get(status.Id)
                              .ConcatIfEmpty(() =>
                              {
                                  StatusStore.Store(status);
                                  return Observable.Return(status);
                              });
        }

        public static IObservable<TwitterStatus> GetTweet(long id)
        {
            return StatusStore.Get(id)
                .Where(_ => _ != null)
                .ConcatIfEmpty(() => GetRandomAuthInfo()
                    .SelectMany(a => a.ShowTweet(id).Do(s => StatusStore.Store(s, false))));
        }

        public static IObservable<TwitterUser> GetUser(long id)
        {
            return UserStore.Get(id)
                .Where(_ => _ != null)
                .ConcatIfEmpty(() => GetRandomAuthInfo().SelectMany(a => a.ShowUser(id).Do(UserStore.Store)));
        }

        public static IObservable<TwitterUser> GetUser(string screenName)
        {
            return UserStore.Get(screenName)
                .Where(_ => _ != null)
                .ConcatIfEmpty(() => GetRandomAuthInfo().SelectMany(a => a.ShowUser(screenName: screenName).Do(UserStore.Store)));
        }

        public static IObservable<AuthenticateInfo> GetRandomAuthInfo()
        {
            return Observable.Defer(() => Observable.Return(AccountsStore.Accounts.Shuffle().FirstOrDefault()))
                .Where(_ => _ != null)
                .Select(_ => _.AuthenticateInfo);
        }
    }
}
