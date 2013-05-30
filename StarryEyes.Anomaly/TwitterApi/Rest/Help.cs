using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Help
    {
        public static async Task<TwitterConfiguration> GetConfiguration(this IOAuthCredential credential)
        {
            var client = credential.CreateOAuthClient();
            var json = await client.GetStringAsync(new ApiAccess("help/configuration.json"));
            return new TwitterConfiguration(DynamicJson.Parse(json));
        }
    }
}
