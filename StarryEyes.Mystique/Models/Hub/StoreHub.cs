using System;
using System.Linq;
using System.Reactive.Linq;
using StarryEyes.Mystique.Models.Store;
using StarryEyes.Mystique.Settings;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Hub
{
    /// <summary>
    /// Find local cache, if not existed, query to Twitter.
    /// </summary>
    public static class StoreHub
    {
        public static IObservable<TwitterStatus> GetTweet(long id)
        {
            return StatusStore.Get(id)
                .Where(_ => _ != null)
                .ConcatIfEmpty(() => GetRandomAuthInfo().SelectMany(a => a.ShowTweet(id).Do(s => StatusStore.Store(s, false))));
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
                .ConcatIfEmpty(() => GetRandomAuthInfo().SelectMany(a => a.ShowUser(screen_name: screenName).Do(UserStore.Store)));
        }

        public static IObservable<AuthenticateInfo> GetRandomAuthInfo()
        {
            return Observable.Defer(() => Observable.Return(Setting.Accounts.Shuffle().FirstOrDefault()))
                .Where(_ => _ != null)
                .Select(_ => _.AuthenticateInfo);
        }
    }
}
