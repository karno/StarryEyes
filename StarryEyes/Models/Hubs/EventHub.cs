using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Backpanels.SystemEvents;
using StarryEyes.Models.Backpanels.TwitterEvents;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Hubs
{
    public static class EventHub
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
                             StatusModel.UpdateStatusInfo(
                                 status.RetweetedOriginal,
                                 model => model.AddRetweetedUser(status.User.Id),
                                 persist =>
                                 persist.RetweetedUsers =
                                 persist.RetweetedUsers.Guard().Append(status.User.Id).ToArray()));
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
                                        model => model.AddFavoritedUser(ue.Source.Id),
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
            OnFollowed += ue => BackpanelModel.RegisterEvent(new FollowedEvent(ue.Source, ue.Target));
            OnUnfollowed += ue => BackpanelModel.RegisterEvent(new UnfollowedEvent(ue.Source, ue.Target));
            OnBlocked += ue => BackpanelModel.RegisterEvent(new BlockedEvent(ue.Source, ue.Target));
            OnUnblocked += ue => BackpanelModel.RegisterEvent(new UnblockedEvent(ue.Source, ue.Target));
            OnFavorited += te => BackpanelModel.RegisterEvent(new FavoritedEvent(te.Source, te.Target));
            OnUnfavorited += te => BackpanelModel.RegisterEvent(new UnfavoritedEvent(te.Source, te.Target));
            OnLimitationInfoGot += (auth, limit) => BackpanelModel.RegisterEvent(new TrackLimitEvent(auth, limit));
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