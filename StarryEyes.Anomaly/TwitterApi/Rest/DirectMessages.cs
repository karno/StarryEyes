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
    public static class DirectMessages
    {
        #region direct_messages

        public static Task<IEnumerable<TwitterStatus>> GetDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, int? count = null, long? sinceId = null, long? maxId = null)
        {
            return credential.GetDirectMessagesAsync(count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, int? count, long? sinceId, long? maxId,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            };
            var resp = await credential.GetAsync("direct_messages.json", param, cancellationToken);
            return await resp.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region direct_messages/sent

        public static Task<IEnumerable<TwitterStatus>> GetSentDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null, int? page = null)
        {
            return credential.GetSentDirectMessagesAsync(count, sinceId, maxId, page, CancellationToken.None);
        }

        public static async Task<IEnumerable<TwitterStatus>> GetSentDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential,
            int? count, long? sinceId, long? maxId, int? page, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"page", page},
            };
            var resp = await credential.GetAsync("direct_messages/sent.json", param, cancellationToken);
            return await resp.ReadAsStatusCollectionAsync();
        }

        #endregion

        #region direct_messages/show

        public static Task<TwitterStatus> ShowDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            return credential.ShowDirectMessageAsync(id, CancellationToken.None);
        }


        public static async Task<TwitterStatus> ShowDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            var resp = await credential.GetAsync("direct_messages/show.json", param, cancellationToken);
            return await resp.ReadAsStatusAsync();
        }

        #endregion

        #region direct_messages/new

        public static Task<TwitterStatus> SendDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter recipient, [NotNull] string text)
        {
            return credential.SendDirectMessageAsync(recipient, text, CancellationToken.None);
        }

        public static async Task<TwitterStatus> SendDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] UserParameter recipient, [NotNull] string text,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (recipient == null) throw new ArgumentNullException("recipient");
            if (text == null) throw new ArgumentNullException("text");
            var param = new Dictionary<string, object>
            {
                {"text", text}
            }.ApplyParameter(recipient);
            var resp = await credential.PostAsync("direct_messages/new.json", param, cancellationToken);
            return await resp.ReadAsStatusAsync();
        }

        #endregion

        #region direct_messages/destroy

        public static Task<TwitterStatus> DestroyDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, long id)
        {
            return credential.DestroyDirectMessageAsync(id, CancellationToken.None);
        }

        public static async Task<TwitterStatus> DestroyDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            var response = await credential.PostAsync("direct_messages/destroy.json",
                param, cancellationToken);
            return await response.ReadAsStatusAsync();
        }

        #endregion
    }
}
