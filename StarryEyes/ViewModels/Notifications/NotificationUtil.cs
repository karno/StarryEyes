using StarryEyes.Nightmare.Windows.Forms;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.Notifications
{
    public static class NotificationUtil
    {
        public static Screen GetNotifyTargetScreen()
        {
            return GetScreenOfIndex(Setting.NotifyScreenIndex.Value);
        }

        public static Screen GetScreenOfIndex(int index)
        {
            if (index >= 0 && index < Screen.AllScreens.Length)
            {
                return Screen.AllScreens[index];
            }
            return Screen.PrimaryScreen;
        }
    }
}
