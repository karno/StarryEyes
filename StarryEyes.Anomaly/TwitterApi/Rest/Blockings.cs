using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Blockings
    {
        #region blocks/ids

        public static Task<ICursorResult<IEnumerable<long>>> GetBlockingsIdsAsync(
            [NotNull] this IOAuthCredential credential, long cursor)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return credential.GetBlockingsIdsAsync(cursor, CancellationToken.None);
        }

        public static async Task<ICursorResult<IEnumerable<long>>> GetBlockingsIdsAsync(
            [NotNull] this IOAuthCredential credential, long cursor, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object> { { "cursor", cursor } };
            var resp = await credential.GetAsync("blocks/ids.json", param, cancellationToken);
            return await resp.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region blocks/create

        public static Task<TwitterUser> CreateBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.CreateBlockAsync(targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> CreateBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var resp = await credential.PostAsync("blocks/create.json",
                targetUser.ToDictionary(), cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion

        #region blocks/destroy

        public static Task<TwitterUser> DestroyBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.DestroyBlockAsync(targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> DestroyBlockAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var resp = await credential.PostAsync("blocks/destroy.json",
                targetUser.ToDictionary(), cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion

        #region users/report_spam

        public static Task<TwitterUser> ReportSpamAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            return credential.ReportSpamAsync(targetUser, CancellationToken.None);
        }

        public static async Task<TwitterUser> ReportSpamAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter targetUser,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (targetUser == null) throw new ArgumentNullException("targetUser");
            var resp = await credential.PostAsync("users/report_spam.json",
                targetUser.ToDictionary(), cancellationToken);
            return await resp.ReadAsUserAsync();
        }

        #endregion
    }
}
