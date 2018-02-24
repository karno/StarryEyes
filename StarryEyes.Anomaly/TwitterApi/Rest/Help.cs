using System;
using System.Threading.Tasks;
using Cadena;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Help
    {
        public static async Task<TwitterConfiguration> GetConfigurationAsync(this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var client = credential.CreateOAuthClient();
            var json = await client.GetStringAsync(new ApiAccess("help/configuration.json"));
            return new TwitterConfiguration(DynamicJson.Parse(json));
        }
    }
}