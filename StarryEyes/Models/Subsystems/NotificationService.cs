using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Feather.Proxies;
using StarryEyes.Filters;
using StarryEyes.Models.Accounting;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Plugins.Injections;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Receiving.Handling;
using StarryEyes.Models.Subsystems.Notifications;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Models.Timelines.Tabs;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems
{
    /// <summary>
    /// Controls around notification.
    /// Delivering entities, Optimizing, Blocking, etc.
    /// </summary>
    public static class NotificationService
    {
        private static INotificationSink _head;
        private static NotificationProxyWrapper _tail;
        private static readonly INotificationSink _sink = new NotificationSink();

        private static INotificationSink Head => _head ?? _sink;

        public static void Initialize()
        {
            // register binder
            BridgeSocketBinder.Bind(NotificationProxy.Socket,
                p => RegisterProxy(new NotificationProxyWrapper(p)));
            NotificationLatch.Initialize();
        }

        public static void RegisterProxy([CanBeNull] NotificationProxyWrapper proxyWrapper)
        {
            if (proxyWrapper == null) throw new ArgumentNullException(nameof(proxyWrapper));
            if (_head == null)
            {
                _head = proxyWrapper;
            }
            else
            {
                _tail.Next = proxyWrapper;
            }
            proxyWrapper.Next = _sink;
            _tail = proxyWrapper;
        }

        #region Notification methods

        internal static void NotifyReceived(TwitterStatus status)
        {
            if (status.RetweetedStatus != null)
            {
                NotifyRetweeted(status.User, status);
            }
            if (status.QuotedStatus != null)
            {
                NotifyQuoted(status.User, status.QuotedStatus, status);
            }
            Head.NotifyReceived(status);
        }

        #region New Arrival Control

        private static readonly Dictionary<long, List<TabModel>> _acceptingArrivals =
            new Dictionary<long, List<TabModel>>();

        internal static void StartAcceptNewArrival(TwitterStatus status)
        {
            // check muted or blocked
            if (MuteBlockManager.IsUnwanted(status)) return;

            if (!Setting.NotifyBackfilledTweets.Value &&
                status.CreatedAt < App.StartupDateTime)
            {
                // backfilled tweets
                return;
            }

            lock (_acceptingArrivals)
            {
                _acceptingArrivals[status.Id] = new List<TabModel>();
            }
        }

        internal static void NotifyNewArrival(TwitterStatus status, TabModel model)
        {
            lock (_acceptingArrivals)
            {
                List<TabModel> list;
                if (_acceptingArrivals.TryGetValue(status.Id, out list))
                {
                    list.Add(model);
                }
            }
        }

        internal static void EndAcceptNewArrival(TwitterStatus status)
        {
            List<TabModel> removal;
            // ignore retweet which mentions me
            var isMention = status.RetweetedStatus == null &&
                            FilterSystemUtil.InReplyToUsers(status)
                                            .Intersect(Setting.Accounts.Ids)
                                            .Any();
            var isMessage = status.StatusType == StatusType.DirectMessage;
            var alwaysAccept = (Setting.NotifyMention.Value && isMention) ||
                               (Setting.NotifyMessage.Value && isMessage);
            lock (_acceptingArrivals)
            {
                if (!_acceptingArrivals.TryGetValue(status.Id, out removal))
                {
                    return;
                }
                // end accept notification
                _acceptingArrivals.Remove(status.Id);

                if (!alwaysAccept && removal.Count == 0)
                {
                    return;
                }
            }
            var soundSource = removal
                .Select(t => t.NotifySoundSource)
                .Where(s => !String.IsNullOrEmpty(s))
                .Where(File.Exists)
                .FirstOrDefault();
            if (isMessage)
            {
                Task.Run(() => NotifyNewArrival(status, NotificationType.DirectMessage, soundSource));
            }
            else if (isMention)
            {
                Task.Run(() => NotifyNewArrival(status, NotificationType.Mention, soundSource));
            }
            else
            {
                Task.Run(() => NotifyNewArrival(status, NotificationType.Normal, soundSource));
            }
        }

        private static void NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
        {
            Head.NotifyNewArrival(status, type, explicitSoundSource);
        }

        #endregion New Arrival Control

        internal static void NotifyFollowed(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetPositive(
                NotificationLatchTarget.Follow, source.Id, target.Id))
                return;
            Head.NotifyFollowed(source, target);
        }

        internal static void NotifyUnfollowed(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetNegative(
                NotificationLatchTarget.Follow, source.Id, target.Id))
                return;
            Head.NotifyUnfollowed(source, target);
        }

        internal static void NotifyBlocked(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetPositive(
                NotificationLatchTarget.Block, source.Id, target.Id))
                return;
            Head.NotifyBlocked(source, target);
        }

        internal static void NotifyUnblocked(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetNegative(
                NotificationLatchTarget.Block, source.Id, target.Id))
                return;
            Head.NotifyUnblocked(source, target);
        }

        internal static void NotifyMuted(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetPositive(
                NotificationLatchTarget.Mute, source.Id, target.Id))
                return;
            Head.NotifyMuted(source, target);
        }

        internal static void NotifyUnmuted(TwitterUser source, TwitterUser target)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetNegative(
                NotificationLatchTarget.Mute, source.Id, target.Id))
                return;
            Head.NotifyUnmuted(source, target);
        }

        internal static void NotifyFavorited(TwitterUser source, TwitterStatus status)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetPositive(
                NotificationLatchTarget.Favorite, source.Id, status.Id))
                return;
            Task.Run(() => UserProxy.StoreUser(source));
            Task.Run(() => StatusModel.UpdateStatusInfo(
                status.Id,
                model => model.AddFavoritedUser(source), _ =>
                {
                    StatusProxy.AddFavoritor(status.Id, source.Id);
                    StatusBroadcaster.Republish(status);
                }));
            Head.NotifyFavorited(source, status);
        }

        internal static void NotifyUnfavorited(TwitterUser source, TwitterStatus status)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetNegative(
                NotificationLatchTarget.Favorite, source.Id, status.Id))
                return;
            Task.Run(() => StatusModel.UpdateStatusInfo(
                status.Id,
                model => model.RemoveFavoritedUser(source.Id), _ =>
                {
                    StatusProxy.RemoveFavoritor(status.Id, source.Id);
                    StatusBroadcaster.Republish(status);
                }));
            Head.NotifyUnfavorited(source, status);
        }

        internal static void NotifyRetweeted(TwitterUser source, TwitterStatus status)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!Setting.NotifyBackfilledTweets.Value && status.CreatedAt < App.StartupDateTime)
            {
                // backfilled tweets
                return;
            }
            var original = status.RetweetedStatus;
            if (original == null) return;
            Task.Run(() => UserProxy.StoreUser(source));
            Task.Run(() => StatusModel.UpdateStatusInfo(
                original.Id,
                model => model.AddRetweetedUser(source), _ =>
                {
                    StatusProxy.AddRetweeter(original.Id, source.Id);
                    StatusBroadcaster.Republish(original);
                }));
            Head.NotifyRetweeted(source, status);
        }

        internal static void NotifyRetweetFavorited(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!NotificationLatch.CheckSetPositive(NotificationLatchTarget.Favorite, source.Id, status.Id))
            {
                return;
            }
            var original = status.RetweetedStatus;
            if (original == null) return;
            Task.Run(() => UserProxy.StoreUser(source));
            Task.Run(() => StatusModel.UpdateStatusInfo(
                original.Id,
                model => model.AddFavoritedUser(source), _ =>
                {
                    StatusProxy.AddFavoritor(original.Id, source.Id);
                    StatusBroadcaster.Republish(original);
                }));
            Head.NotifyRetweetFavorited(source, target, status);
        }

        internal static void NotifyRetweetRetweeted(TwitterUser source, TwitterUser target, TwitterStatus status)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!Setting.NotifyBackfilledTweets.Value && status.CreatedAt < App.StartupDateTime)
            {
                // backfilled tweets
                return;
            }
            var original = status.RetweetedStatus;
            if (original == null) return;
            Task.Run(() => UserProxy.StoreUser(source));
            Task.Run(() => StatusModel.UpdateStatusInfo(
                original.Id,
                model => model.AddRetweetedUser(source), _ =>
                {
                    StatusProxy.AddRetweeter(original.Id, source.Id);
                    StatusBroadcaster.Republish(original);
                }));
            Head.NotifyRetweetRetweeted(source, target, status);
        }

        internal static void NotifyQuoted(TwitterUser source, TwitterStatus original, TwitterStatus quote)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            if (!Setting.NotifyBackfilledTweets.Value &&
                quote.CreatedAt < App.StartupDateTime)
            {
                // backfilled tweets
                return;
            }
            Task.Run(() => UserProxy.StoreUser(source));
            Head.NotifyQuoted(source, original, quote);
        }

        internal static void NotifyDeleted(long statusId, TwitterStatus deleted)
        {
            if (deleted?.RetweetedStatus != null)
            {
                Task.Run(() => StatusModel.UpdateStatusInfo(
                    deleted.RetweetedStatus.Id,
                    model => model.RemoveRetweetedUser(deleted.User.Id),
                    _ => StatusProxy.RemoveRetweeter(deleted.RetweetedStatus.Id,
                        deleted.User.Id)));
            }
            Head.NotifyDeleted(statusId, deleted);
        }

        internal static void NotifyLimitationInfoGot(TwitterAccount account, int trackLimit)
        {
            Head.NotifyLimitationInfoGot(account, trackLimit);
        }

        internal static void NotifyUserUpdated(TwitterUser source)
        {
            if (MuteBlockManager.IsBlocked(source) || MuteBlockManager.IsOfficialMuted(source)) return;
            Head.NotifyUserUpdated(source);
        }

        #endregion Notification methods
    }
}