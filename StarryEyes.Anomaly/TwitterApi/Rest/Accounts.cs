using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Accounts
    {
        #region account/verify_credentials

        public static Task<TwitterUser> VerifyCredentialAsync(
            [NotNull] this IOAuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.VerifyCredentialAsync(CancellationToken.None);
        }

        public static async Task<TwitterUser> VerifyCredentialAsync(
            [NotNull] this IOAuthCredential credential, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"skip_status", true}
            };
            var resp = await credential.GetAsync(
                "account/verify_credentials.json", param, cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion

        #region account/update_profile

        public static Task<TwitterUser> UpdateProfileAsync(
            [NotNull] this IOAuthCredential credential,
            [CanBeNull] string name = null, [CanBeNull] string url = null, [CanBeNull] string location = null,
            [CanBeNull] string description = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.UpdateProfileAsync(name, url, location, description,
                CancellationToken.None);
        }

        public static async Task<TwitterUser> UpdateProfileAsync(
            [NotNull] this IOAuthCredential credential,
            [CanBeNull] string name, [CanBeNull] string url, [CanBeNull] string location, [CanBeNull] string description,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"name", name},
                {"url", url},
                {"location", location},
                {"description", description},
                {"skip_status", true},
            };
            var resp = await credential.PostAsync(
                "account/update_profile.json",
                param, cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion

        #region account/update_profile_image

        public static Task<TwitterUser> UpdateProfileImageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] byte[] image)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (image == null) throw new ArgumentNullException("image");
            return credential.UpdateProfileImageAsync(image, CancellationToken.None);
        }

        public static async Task<TwitterUser> UpdateProfileImageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] byte[] image,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new StringContent("true"), "skip_status"},
                {new ByteArrayContent(image), "image", "image.png"}
            };
            var resp = await credential.PostAsync(
                "account/update_profile_image.json",
                content, cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion
    }
}