using StarryEyes.Fragments;
using StarryEyes.Fragments.Proxies;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Subsystems.Notifications;

namespace StarryEyes.Models.Plugins
{
    public sealed class BridgeImpl : IBridge
    {
        private readonly NotificationProxyManager _npm;

        public BridgeImpl()
        {
            this._npm = new NotificationProxyManager();
        }

        public INotificationProxyManager NotificationProxyManager
        {
            get { return _npm; }
        }
    }

    public sealed class NotificationProxyManager : INotificationProxyManager
    {
        public void Register(INotificationProxy proxy)
        {
            NotificationService.RegisterProxy(new NotificationProxyWrapper(proxy));
        }
    }
}
