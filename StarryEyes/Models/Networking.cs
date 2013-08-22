using System;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    public static class Networking
    {
        internal static void Initialize()
        {
            Setting.IsBypassWebProxyInLocal.ValueChanged += _ => ApplyWebProxy();
            Setting.UseWebProxy.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyAddress.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyBypassList.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyPort.ValueChanged += _ => ApplyWebProxy();
            ApplyWebProxy();
        }

        public static void ApplyWebProxy()
        {
            throw new NotImplementedException();
        }
    }
}
