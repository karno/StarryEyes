using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Accounts
    {
        public static async Task<TwitterUser> VerifyCredentialAsync(
            this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"skip_status", true}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("account/verify_credentials.json", param));
            return await response.ReadAsUserAsync();
        }

        public static async Task<TwitterUser> UpdateProfileAsync(
            this IOAuthCredential credential,
            string name = null, string url = null,
            string location = null, string description = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"name", name},
                {"url", url},
                {"location", location},
                {"description", description},
                {"skip_status", true},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("account/update_profile.json"), param);
            return await response.ReadAsUserAsync();
        }

        public static async Task<TwitterUser> UpdateProfileImageAsync(
            this IOAuthCredential credential,
            byte[] image)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new StringContent("true"), "skip_status"},
                {new ByteArrayContent(image), "image", "image.png"}
            };
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("account/update_profile_image.json"), content);
            return await response.ReadAsUserAsync();
        }
    }
}