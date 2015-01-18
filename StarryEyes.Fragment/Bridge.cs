using System.Windows;
using StarryEyes.Fragments.Proxies;

namespace StarryEyes.Fragments
{
    public static class Bridge
    {
        public static IBridge Get()
        {
            var bridge = Application.Current as IBridgeProvider;
            return bridge == null ? null : bridge.GetBridge();
        }
    }

    public interface IBridgeProvider
    {
        IBridge GetBridge();
    }

    public interface IBridge
    {
        INotificationProxyManager NotificationProxyManager { get; }
    }
}
