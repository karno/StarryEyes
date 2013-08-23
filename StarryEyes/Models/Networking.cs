using System;
using System.Globalization;
using System.Net;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Models.Backstages.NotificationEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    public static class Networking
    {
        internal static void Initialize()
        {
            Setting.IsBypassWebProxyInLocal.ValueChanged += _ => ApplyWebProxy();
            Setting.UseWebProxy.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyHost.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyBypassList.ValueChanged += _ => ApplyWebProxy();
            Setting.WebProxyPort.ValueChanged += _ => ApplyWebProxy();
            ApplyWebProxy();
            Setting.UserAgent.ValueChanged += _ => ApplyBaseProperties();
            Setting.ApiProxy.ValueChanged += _ => ApplyBaseProperties();
            ApplyBaseProperties();
        }

        public static void ApplyWebProxy()
        {
            switch (Setting.UseWebProxy.Value)
            {
                case WebProxyConfiguration.Default:
                    Anomaly.Core.UseSystemProxy = true;
                    break;
                case WebProxyConfiguration.None:
                    Anomaly.Core.UseSystemProxy = false;
                    Anomaly.Core.ProxyProvider = null;
                    break;
                case WebProxyConfiguration.Custom:
                    Anomaly.Core.UseSystemProxy = false;
                    try
                    {
                        Anomaly.Core.ProxyProvider =
                            () => new WebProxy(
                                      new Uri("http://" + Setting.WebProxyHost.Value + ":" +
                                              Setting.WebProxyPort.Value.ToString(CultureInfo.InvariantCulture)),
                                      Setting.IsBypassWebProxyInLocal.Value,
                                      Setting.WebProxyBypassList.Value);
                    }
                    catch (Exception ex)
                    {
                        Anomaly.Core.ProxyProvider = null;
                        BackstageModel.RegisterEvent(new OperationFailedEvent("PROXY ERROR: " + ex.Message));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void ApplyBaseProperties()
        {
            // when set null or empty string, apply defaults.
            ApiAccessProperties.UserAgent = Setting.UserAgent.Value;
            ApiAccessProperties.ApiEndpoint = Setting.ApiProxy.Value;
        }
    }
}
