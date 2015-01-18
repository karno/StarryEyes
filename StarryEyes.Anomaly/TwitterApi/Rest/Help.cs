using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Help
    {
        public static async Task<TwitterConfiguration> GetConfigurationAsync(this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var json = await credential.GetStringAsync("help/configuration.json",
                new Dictionary<string, object>(), CancellationToken.None);
            return new TwitterConfiguration(DynamicJson.Parse(json));
        }

        public static async Task<TwitterConfiguration> GetConfigurationAsync(
            this IOAuthCredential credential, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var json = await credential.GetStringAsync("help/configuration.json",
                new Dictionary<string, object>(), cancellationToken);
            return new TwitterConfiguration(DynamicJson.Parse(json));
        }
    }
}
