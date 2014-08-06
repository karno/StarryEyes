using System;
using System.IO;
using System.Threading.Tasks;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Fragments.Proxies;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Subsystems.Notifications.Audio;
using StarryEyes.Settings;

namespace StarryEyes.Models.Subsystems.Notifications.UI
{
    public static class UINotificationProxy
    {
        /// <summary>
        /// Initialize notification router
        /// </summary>
        public static void Initialize()
        {
            NotificationService.RegisterProxy(
                new NotificationProxyWrapper(new ΝotificationViewRouter()));
        }

        private static IUINotificator GetNotificator()
        {
            switch (Setting.NotificationType.Value)
            {
                case NotificationUIType.Popup:
                    return NormalNotificator.Instance;
                case NotificationUIType.Slim:
                    return SlimNotificator.Instance;
                default:
                    return NullNotificator.Instance;
            }
        }

        private static string GetSoundFilePath(NotifySoundType type)
        {
            string file;
            switch (type)
            {
                case NotifySoundType.New:
                    file = App.NewReceiveWavFile;
                    break;
                case NotifySoundType.Mention:
                    file = App.MentionWavFile;
                    break;
                case NotifySoundType.Message:
                    file = App.DirectMessageWavFile;
                    break;
                case NotifySoundType.Event:
                    file = App.EventWavFile;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            return Path.Combine(App.ExeFileDir, App.MediaDirectory, file);
        }

        private static void PlaySoundCore(string filePath)
        {
            if (!Setting.PlaySounds.Value || String.IsNullOrEmpty(filePath) ||
                !File.Exists(filePath))
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    AudioPlayer.PlaySound(filePath);
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("SOUND ERROR", ex));
                }
            });
        }

        private class ΝotificationViewRouter : INotificationProxy
        {
            private bool CheckMyself(TwitterStatus status)
            {
                return CheckMyself(status.User);
            }

            private bool CheckMyself(TwitterUser user)
            {
                return Setting.Accounts.Contains(user.Id);
            }

            public bool NotifyReceived(TwitterStatus status)
            {
                // do nothing
                return false;
            }

            public bool NotifyNewArrival(TwitterStatus status, NotificationType type, string explicitSoundSource)
            {
                if (CheckMyself(status))
                {
                    return false;
                }

                // convert to default notification ?
                if (type == NotificationType.Mention && !Setting.NotifyMention.Value)
                {
                    type = NotificationType.Normal;
                }
                if (type == NotificationType.DirectMessage && !Setting.NotifyMessage.Value)
                {
                    type = NotificationType.Normal;
                }

                if (status.RetweetedOriginal != null && CheckMyself(status.RetweetedOriginal))
                {
                    // suppress status which retweets our tweet
                    // -> notify as "our status is retweeted"
                    return false;
                }
                System.Diagnostics.Debug.WriteLine("New Arrival: " + type + " - " + status);
                if (!String.IsNullOrEmpty(explicitSoundSource) && File.Exists(explicitSoundSource))
                {
                    PlaySoundCore(explicitSoundSource);
                }
                else
                {
                    PlaySoundCore(GetSoundFilePath((NotifySoundType)type));
                }
                switch (type)
                {
                    case NotificationType.Normal:
                        GetNotificator().StatusReceived(status);
                        break;
                    case NotificationType.Mention:
                        GetNotificator().MentionReceived(status);
                        break;
                    case NotificationType.DirectMessage:
                        GetNotificator().MessageReceived(status);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }
                return false;
            }

            public bool NotifyFollowed(TwitterUser source, TwitterUser target)
            {
                if (Setting.NotifyFollow.Value &&
                    !CheckMyself(source) && CheckMyself(target))
                {
                    PlaySoundCore(GetSoundFilePath(NotifySoundType.Event));
                    GetNotificator().Followed(source, target);
                }
                return false;
            }

            public bool NotifyUnfollowed(TwitterUser source, TwitterUser target)
            {
                return false;
            }

            public bool NotifyBlocked(TwitterUser source, TwitterUser target)
            {
                return false;
            }

            public bool NotifyUnblocked(TwitterUser source, TwitterUser target)
            {
                return false;
            }

            public bool NotifyFavorited(TwitterUser source, TwitterStatus status)
            {
                if (Setting.NotifyFavorite.Value &&
                    !CheckMyself(source) && CheckMyself(status))
                {
                    PlaySoundCore(GetSoundFilePath(NotifySoundType.Event));
                    GetNotificator().Favorited(source, status);
                }
                return false;
            }

            public bool NotifyUnfavorited(TwitterUser source, TwitterStatus status)
            {
                return false;
            }

            public bool NotifyRetweeted(TwitterUser source, TwitterStatus original, TwitterStatus retweet)
            {
                if (Setting.NotifyRetweet.Value &&
                    !CheckMyself(source) && CheckMyself(original))
                {
                    PlaySoundCore(GetSoundFilePath(NotifySoundType.Event));
                    GetNotificator().Retweeted(source, original, retweet);
                }
                return false;
            }

            public bool NotifyDeleted(long statusId, TwitterStatus deleted)
            {
                return false;
            }

            public bool NotifyLimitationInfoGot(IOAuthCredential account, int trackLimit)
            {
                return false;
            }

            public bool NotifyUserUpdated(TwitterUser source)
            {
                return false;
            }
        }

        enum NotifySoundType
        {
            New,
            Mention,
            Message,
            Event
        }
    }

    public enum NotificationUIType
    {
        None,
        Slim,
        Popup
    }
}
