using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    public static class TwitterEventService
    {
        public static bool OverrideDefaultHandlers { get; set; }

        internal static void Initialize()
        {
            // incoming events
            StatusStore.StatusPublisher
                       .Where(n => n.IsAdded)
                       .Select(n => n.Status)
                       .Subscribe(NotifyReceived);
            StatusStore.StatusPublisher
                       .Where(n => !n.IsAdded && n.Status != null)
                       .Select(n => n.Status)
                       .Subscribe(NotifyDeleted);
            // required default handlers
            Received += status =>
            {
                if (status.RetweetedOriginal != null)
                {
                    Task.Run(() =>
                    {
                        var isAdd = false;
                        StatusModel.UpdateStatusInfo(
                            status.RetweetedOriginal,
                            model =>
                            {
                                if (model.IsRetweeted(status.User.Id)) return;
                                isAdd = true;
                                model.AddRetweetedUser(status.User);
                            },
                            persist =>
                            {
                                if (persist.RetweetedUsers == null)
                                {
                                    isAdd = true;
                                    persist.RetweetedUsers = new[] { status.User.Id };
                                }
                                else if (!persist.RetweetedUsers.Contains(status.User.Id))
                                {
                                    isAdd = true;
                                    persist.RetweetedUsers =
                                        persist.RetweetedUsers.Guard().Append(status.User.Id).ToArray();
                                }
#pragma warning disable 4014
                                DatabaseProxy.AddRetweeterAsync(persist.Id, status.User.Id);
#pragma warning restore 4014
                            });
                        if (isAdd)
                            NotifyRetweeted(status.User, status.RetweetedOriginal);
                    });
                }
            };
            Deleted += status =>
            {
                if (status.RetweetedOriginal != null)
                {
                    Task.Run(() =>
                             StatusModel.UpdateStatusInfo(
                                 status.RetweetedOriginal,
                                 model => model.RemoveRetweetedUser(status.User.Id),
                                 persist =>
                                 {
                                     DatabaseProxy.RemoveRetweeterAsync(persist.Id, status.User.Id);
                                     persist.RetweetedUsers =
                                         persist.RetweetedUsers.Guard().Where(id => id != status.User.Id).ToArray();
                                 }));
                }
            };
            Favorited += ue =>
                         Task.Run(() =>
                                  StatusModel.UpdateStatusInfo(
                                      ue.Target,
                                      model => model.AddFavoritedUser(ue.Source),
                                      status =>
                                      {
                                          DatabaseProxy.AddFavoritorAsync(status.Id, ue.Source.Id);
                                          status.FavoritedUsers =
                                              status.FavoritedUsers.Guard()
                                                    .Where(id => id != ue.Source.Id)
                                                    .ToArray();
                                      }));
            Unfavorited += ue =>
                             Task.Run(() =>
                                      StatusModel.UpdateStatusInfo(
                                          ue.Target,
                                          model => model.RemoveFavoritedUser(ue.Source.Id),
                                          status =>
                                          {
                                              DatabaseProxy.RemoveFavoritorAsync(status.Id, ue.Source.Id);
                                              status.FavoritedUsers =
                                                  status.FavoritedUsers.Guard()
                                                        .Where(id => id != ue.Source.Id)
                                                        .ToArray();
                                          }));
        }

        internal static void RegisterDefaultHandlers()
        {
            // optional default handlers
            if (OverrideDefaultHandlers) return;
            Followed += ue => BackstageModel.RegisterEvent(new FollowedEvent(ue.Source, ue.Target));
            Unfollowed += ue => BackstageModel.RegisterEvent(new UnfollowedEvent(ue.Source, ue.Target));
            Blocked += ue => BackstageModel.RegisterEvent(new BlockedEvent(ue.Source, ue.Target));
            Unblocked += ue => BackstageModel.RegisterEvent(new UnblockedEvent(ue.Source, ue.Target));
            Favorited += te => BackstageModel.RegisterEvent(new FavoritedEvent(te.Source, te.Target));
            Unfavorited += te => BackstageModel.RegisterEvent(new UnfavoritedEvent(te.Source, te.Target));
            Retweeted += te =>
            {
                // check user
                if (Setting.Accounts.Contains(te.Source.Id) ||
                    Setting.Accounts.Contains(te.Target.User.Id))
                {
                    BackstageModel.RegisterEvent(new RetweetedEvent(te.Source, te.Target));
                }
            };
            LimitationInfoGot += (auth, limit) => BackstageModel.RegisterEvent(new TrackLimitEvent(auth, limit));
        }

        public static event Action<TwitterStatus> Received;

        public static void NotifyReceived(TwitterStatus status)
        {
            var handler = Received;
            if (handler != null)
                handler(status);
        }

        public static event Action<TwitterUserEvent> Followed;

        public static void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            var handler = Followed;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> Unfollowed;

        public static void NotifyUnfollwed(TwitterUser source, TwitterUser target)
        {
            var handler = Unfollowed;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> Blocked;

        public static void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            var handler = Blocked;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> Unblocked;

        public static void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            var handler = Unblocked;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterStatusEvent> Favorited;

        public static void NotifyFavorited(TwitterUser source, TwitterStatus target)
        {
            var handler = Favorited;
            if (handler != null)
                handler(new TwitterStatusEvent(source, target));
        }

        public static event Action<TwitterStatusEvent> Unfavorited;

        public static void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            var handler = Unfavorited;
            if (handler != null)
                handler(new TwitterStatusEvent(source, status));
        }

        public static event Action<TwitterStatusEvent> Retweeted;

        public static void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            var handler = Retweeted;
            if (handler != null)
                handler(new TwitterStatusEvent(source, status));
        }

        public static event Action<TwitterStatus> Deleted;

        public static void NotifyDeleted(TwitterStatus deleted)
        {
            var handler = Deleted;
            if (handler != null)
                handler(deleted);
        }

        public static event Action<TwitterAccount, int> LimitationInfoGot;

        public static void NotifyLimitationInfoGot(TwitterAccount account, int trackLimit)
        {
            var handler = LimitationInfoGot;
            if (handler != null)
                handler(account, trackLimit);
        }

        public static event Action<TwitterUser> UserProfileUpdated;

        public static void NotifyUserUpdated(TwitterUser source)
        {
            var handler = UserProfileUpdated;
            if (handler != null) handler(source);
        }
    }

    public class TwitterUserEvent
    {
        public TwitterUserEvent(TwitterUser source, TwitterUser target)
        {
            this.Source = source;
            this.Target = target;
        }

        public TwitterUser Source { get; set; }
        public TwitterUser Target { get; set; }
    }

    public class TwitterStatusEvent
    {
        public TwitterStatusEvent(TwitterUser source, TwitterStatus target)
        {
            this.Source = source;
            this.Target = target;
        }

        public TwitterUser Source { get; set; }
        public TwitterStatus Target { get; set; }
    }
}