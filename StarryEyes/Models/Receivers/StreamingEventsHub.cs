using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Receivers
{
    public static class StreamingEventsHub
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
            OnReceived += status =>
            {
                if (status.RetweetedOriginal != null)
                {
                    Task.Run(() =>
                    {
                        bool isAdd = false;
                        StatusModel.UpdateStatusInfo(
                            status.RetweetedOriginal,
                            model =>
                            {
                                if (!model.IsRetweeted(status.User.Id))
                                {
                                    isAdd = true;
                                    model.AddRetweetedUser(status.User);
                                }
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
                            });
                        if (isAdd)
                            NotifyRetweeted(status.User, status.RetweetedOriginal);
                    });
                }
            };
            OnDeleted += status =>
            {
                if (status.RetweetedOriginal != null)
                {
                    Task.Run(() =>
                             StatusModel.UpdateStatusInfo(
                                 status.RetweetedOriginal,
                                 model => model.RemoveRetweetedUser(status.User.Id),
                                 persist =>
                                 persist.RetweetedUsers =
                                 persist.RetweetedUsers.Guard().Where(id => id != status.User.Id).ToArray()));
                }
            };
            OnFavorited += ue =>
                           Task.Run(() =>
                                    StatusModel.UpdateStatusInfo(
                                        ue.Target,
                                        model => model.AddFavoritedUser(ue.Source),
                                        status =>
                                        status.FavoritedUsers =
                                        status.FavoritedUsers.Guard().Where(id => id != ue.Source.Id).ToArray()));
            OnUnfavorited += ue =>
                             Task.Run(() =>
                                      StatusModel.UpdateStatusInfo(
                                          ue.Target,
                                          model => model.RemoveFavoritedUser(ue.Source.Id),
                                          status =>
                                          status.FavoritedUsers =
                                          status.FavoritedUsers.Guard().Where(id => id != ue.Source.Id).ToArray()));
        }

        internal static void RegisterDefaultHandlers()
        {
            // optional default handlers
            if (OverrideDefaultHandlers) return;
            OnFollowed += ue => BackstageModel.RegisterEvent(new FollowedEvent(ue.Source, ue.Target));
            OnUnfollowed += ue => BackstageModel.RegisterEvent(new UnfollowedEvent(ue.Source, ue.Target));
            OnBlocked += ue => BackstageModel.RegisterEvent(new BlockedEvent(ue.Source, ue.Target));
            OnUnblocked += ue => BackstageModel.RegisterEvent(new UnblockedEvent(ue.Source, ue.Target));
            OnFavorited += te => BackstageModel.RegisterEvent(new FavoritedEvent(te.Source, te.Target));
            OnUnfavorited += te => BackstageModel.RegisterEvent(new UnfavoritedEvent(te.Source, te.Target));
            OnRetweeted += te =>
            {
                // check user
                if (AccountsStore.AccountIds.Contains(te.Source.Id) ||
                    AccountsStore.AccountIds.Contains(te.Target.User.Id))
                {
                    BackstageModel.RegisterEvent(new RetweetedEvent(te.Source, te.Target));
                }
            };
            OnLimitationInfoGot += (auth, limit) => BackstageModel.RegisterEvent(new TrackLimitEvent(auth, limit));
        }

        public static event Action<TwitterStatus> OnReceived;

        public static void NotifyReceived(TwitterStatus status)
        {
            Action<TwitterStatus> handler = OnReceived;
            if (handler != null)
                handler(status);
        }

        public static event Action<TwitterUserEvent> OnFollowed;

        public static void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            Action<TwitterUserEvent> handler = OnFollowed;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> OnUnfollowed;

        public static void NotifyUnfollwed(TwitterUser source, TwitterUser target)
        {
            Action<TwitterUserEvent> handler = OnUnfollowed;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> OnBlocked;

        public static void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            Action<TwitterUserEvent> handler = OnBlocked;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterUserEvent> OnUnblocked;

        public static void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            Action<TwitterUserEvent> handler = OnUnblocked;
            if (handler != null)
                handler(new TwitterUserEvent(source, target));
        }

        public static event Action<TwitterStatusEvent> OnFavorited;

        public static void NotifyFavorited(TwitterUser source, TwitterStatus target)
        {
            Action<TwitterStatusEvent> handler = OnFavorited;
            if (handler != null)
                handler(new TwitterStatusEvent(source, target));
        }

        public static event Action<TwitterStatusEvent> OnUnfavorited;

        public static void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            Action<TwitterStatusEvent> handler = OnUnfavorited;
            if (handler != null)
                handler(new TwitterStatusEvent(source, status));
        }

        public static event Action<TwitterStatusEvent> OnRetweeted;

        public static void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            Action<TwitterStatusEvent> handler = OnRetweeted;
            if (handler != null)
                handler(new TwitterStatusEvent(source, status));
        }

        public static event Action<TwitterStatus> OnDeleted;

        public static void NotifyDeleted(TwitterStatus deleted)
        {
            Action<TwitterStatus> handler = OnDeleted;
            if (handler != null)
                handler(deleted);
        }

        public static event Action<AuthenticateInfo, int> OnLimitationInfoGot;

        public static void NotifyLimitationInfoGot(AuthenticateInfo auth, int trackLimit)
        {
            Action<AuthenticateInfo, int> handler = OnLimitationInfoGot;
            if (handler != null)
                handler(auth, trackLimit);
        }
    }

    public class TwitterUserEvent
    {
        public TwitterUserEvent(TwitterUser source, TwitterUser target)
        {
            Source = source;
            Target = target;
        }

        public TwitterUser Source { get; set; }
        public TwitterUser Target { get; set; }
    }

    public class TwitterStatusEvent
    {
        public TwitterStatusEvent(TwitterUser source, TwitterStatus target)
        {
            Source = source;
            Target = target;
        }

        public TwitterUser Source { get; set; }
        public TwitterStatus Target { get; set; }
    }
}