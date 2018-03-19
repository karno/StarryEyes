using Cadena;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Feather.Proxies;

namespace StarryEyes.Models.Subsystems.Notifications
{
    public interface INotificationSink
    {
        void NotifyReceived([NotNull] TwitterStatus status);

        void NotifyNewArrival([NotNull] TwitterStatus status, NotificationType type,
            [CanBeNull] string explicitSoundSource);

        void NotifyFollowed([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyUnfollowed([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyBlocked([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyUnblocked([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyMuted([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyUnmuted([NotNull] TwitterUser source, [NotNull] TwitterUser target);

        void NotifyFavorited([NotNull] TwitterUser source, [NotNull] TwitterStatus status);

        void NotifyUnfavorited([NotNull] TwitterUser source, [NotNull] TwitterStatus status);

        void NotifyRetweeted([NotNull] TwitterUser source, [NotNull] TwitterStatus original,
            [NotNull] TwitterStatus retweet, bool retweetedRetweet);

        void NotifyQuoted([NotNull] TwitterUser source, [NotNull] TwitterStatus original,
            [NotNull] TwitterStatus quote);

        void NotifyDeleted(long statusId, [CanBeNull] TwitterStatus deleted);

        void NotifyLimitationInfoGot([NotNull] IOAuthCredential account, int trackLimit);

        void NotifyUserUpdated([NotNull] TwitterUser source);
    }
}