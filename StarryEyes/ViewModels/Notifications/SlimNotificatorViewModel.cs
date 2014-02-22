
using StarryEyes.Models.Subsystems.Notifications.UI;

namespace StarryEyes.ViewModels.Notifications
{
    public class SlimNotificatorViewModel
    {
        private static bool _isDisplaying = false;

        public static void Initialize()
        {
            SlimNotificator.Instance.OnNewNotificationDataQueued += () =>
            {
                if (!_isDisplaying)
                {
                    _isDisplaying = true;
                    ShowNext();
                }
            };
        }

        private static void ShowNext()
        {
            var next = SlimNotificator.Instance.GetQueuedNotification();
            if (next != null)
            {
                Show(next);
            }
            else
            {
                _isDisplaying = false;
            }
        }

        private static void Show(NotificationData next)
        {
        }
    }
}
