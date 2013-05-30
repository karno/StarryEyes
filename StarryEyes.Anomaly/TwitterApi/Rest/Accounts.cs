using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi.DataModels;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Accounts
    {
        public static async Task<TwitterUser> VerifyCredential(
            this IOAuthCredential credential)
        {
            var client = credential.CreateOAuthClient();
            var json = await client.GetStringAsync(new ApiAccess("account/verify_credentials.json"));
            return new TwitterUser(DynamicJson.Parse(json));
        }

        public static async Task<TwitterUser> UpdateProfile(
            this IOAuthCredential credential,
            string name = null, string url = null,
            string location = null, string description = null)
        {
            var param = new Dictionary<string, object>()
            {
                {"name", name},
                {"url", url},
                {"location", location},
                {"description", description},
            }.Parametalize();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("account/update_profile.json"), param);
            var json = await response.Content.ReadAsStringAsync();
            return new TwitterUser(DynamicJson.Parse(json));
        }

        // TODO: UpdateProfileImage
    }
}