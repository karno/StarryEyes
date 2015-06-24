using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Help
    {
        public static Task<IApiResult<TwitterConfiguration>> GetConfigurationAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetConfigurationAsync(properties, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterConfiguration>> GetConfigurationAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return await credential.GetAsync(properties, "help/configuration.json",
                new Dictionary<string, object>(), ResultHandlers.ReadAsConfigurationAsync,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
