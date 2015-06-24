using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Accounts
    {
        #region account/verify_credentials

        public static Task<IApiResult<TwitterUser>> VerifyCredentialAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.VerifyCredentialAsync(properties, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> VerifyCredentialAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"skip_status", true}
            };

            return await credential.GetAsync(properties, "account/verify_credentials.json",
                param, ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region account/update_profile

        public static Task<IApiResult<TwitterUser>> UpdateProfileAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] string name = null, [CanBeNull] string url = null, [CanBeNull] string location = null,
            [CanBeNull] string description = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.UpdateProfileAsync(properties, name, url, location, description,
                CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> UpdateProfileAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [CanBeNull] string name, [CanBeNull] string url, [CanBeNull] string location, [CanBeNull] string description,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"name", name},
                {"url", url},
                {"location", location},
                {"description", description},
                {"skip_status", true},
            };
            return await credential.PostAsync(properties, "account/update_profile.json",
                param, ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region account/update_profile_image

        public static Task<IApiResult<TwitterUser>> UpdateProfileImageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] byte[] image)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (image == null) throw new ArgumentNullException("image");
            return credential.UpdateProfileImageAsync(properties, image, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterUser>> UpdateProfileImageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] byte[] image, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (image == null) throw new ArgumentNullException("image");
            var content = new MultipartFormDataContent
            {
                {new StringContent("true"), "skip_status"},
                {new ByteArrayContent(image), "image", "image.png"}
            };
            return await credential.PostAsync(properties, "account/update_profile_image.json",
                content, ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}