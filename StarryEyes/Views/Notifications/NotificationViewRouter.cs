using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using NAudio.Wave;
using StarryEyes.Anomaly;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Feather.Proxies;
using StarryEyes.Models;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Subsystems.Notifications;
using StarryEyes.Settings;

namespace StarryEyes.Views.Notifications
{
    public static class NotificationViewRouter
    {
        /// <summary>
        /// Initialize notification router
        /// </summary>
        public static void Initialize()
        {
            NotificationService.RegisterProxy(
                new NotificationProxyWrapper(new ΝotificationViewRouter()));
        }

        private static INotificator GetNotificator()
        {
            return NullNotificator.Instance;
        }

        private static string GetSoundFilePath(NotifySoundType type)
        {
            var file = String.Empty;
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
                var disposables = new CompositeDisposable();
                try
                {
                    // initialize classes
                    var reader = new WaveFileReader(filePath);
                    disposables.Add(reader);

                    var waveChannel = new WaveChannel32(reader);
                    disposables.Add(waveChannel);
                    waveChannel.PadWithZeroes = false;

                    var player = new DirectSoundOut();
                    disposables.Add(player);
                    player.Init(waveChannel);
                    player.PlaybackStopped += (o, e) => disposables.Dispose();

                    player.Play();
                }
                catch (Exception ex)
                {
                    BackstageModel.RegisterEvent(new OperationFailedEvent("SOUND ERROR", ex));
                    // cleanup resources
                    disposables.Dispose();
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
                if (!CheckMyself(source) && CheckMyself(target))
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
                if (!CheckMyself(source) && CheckMyself(status))
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

            public bool NotifyRetweeted(TwitterUser source, TwitterStatus status)
            {
                if (!CheckMyself(source) && CheckMyself(status))
                {
                    PlaySoundCore(GetSoundFilePath(NotifySoundType.Event));
                    GetNotificator().Retweeted(source, status);
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
}
