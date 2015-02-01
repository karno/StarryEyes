using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Internals;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameters;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Blockings
    {
        #region blocks/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetBlockingsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long cursor)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetBlockingsIdsAsync(properties, cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetBlockingsIdsAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object> { { "cursor", cursor } };
            return await credential.GetAsync(properties, "blocks/ids.json", param,
                ResultHandlers.ReadAsCursoredIdsAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region blocks/create

        public static Task<TwitterUser> CreateBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.CreateBlockAsync(properties, targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> CreateBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return await credential.PostAsync(properties, "blocks/create.json", targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region blocks/destroy

        public static Task<TwitterUser> DestroyBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.DestroyBlockAsync(properties, targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> DestroyBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return await credential.PostAsync(properties, "blocks/destroy.json", targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region users/report_spam

        public static Task<TwitterUser> ReportSpamAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.ReportSpamAsync(properties, targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> ReportSpamAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter targetUser, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return await credential.PostAsync(properties, "users/report_spam.json", targetUser.ToDictionary(),
                ResultHandlers.ReadAsUserAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
