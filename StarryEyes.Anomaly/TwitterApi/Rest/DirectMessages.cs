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
    public static class DirectMessages
    {
        #region direct_messages

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetDirectMessagesAsync(properties, count, sinceId, maxId, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count, long? sinceId, long? maxId, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            };
            return await credential.GetAsync(properties, "direct_messages.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region direct_messages/sent

        public static Task<IApiResult<IEnumerable<TwitterStatus>>> GetSentDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count = null, long? sinceId = null, long? maxId = null, int? page = null)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.GetSentDirectMessagesAsync(properties, count, sinceId, maxId, page, CancellationToken.None);
        }

        public static async Task<IApiResult<IEnumerable<TwitterStatus>>> GetSentDirectMessagesAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            int? count, long? sinceId, long? maxId, int? page, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"page", page},
            };
            return await credential.GetAsync(properties, "direct_messages/sent.json", param,
                ResultHandlers.ReadAsStatusCollectionAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region direct_messages/show

        public static Task<IApiResult<TwitterStatus>> ShowDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.ShowDirectMessageAsync(properties, id, CancellationToken.None);
        }


        public static async Task<IApiResult<TwitterStatus>> ShowDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            long id, CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            return await credential.GetAsync(properties, "direct_messages/show.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region direct_messages/new

        public static Task<IApiResult<TwitterStatus>> SendDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter recipient, [NotNull] string text)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.SendDirectMessageAsync(properties, recipient, text, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterStatus>> SendDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties,
            [NotNull] UserParameter recipient, [NotNull] string text,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (properties == null) throw new ArgumentNullException("properties");
            if (recipient == null) throw new ArgumentNullException("recipient");
            if (text == null) throw new ArgumentNullException("text");
            var param = new Dictionary<string, object>
            {
                {"text", text}
            }.ApplyParameter(recipient);
            return await credential.PostAsync(properties, "direct_messages/new.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region direct_messages/destroy

        public static Task<IApiResult<TwitterStatus>> DestroyDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, [NotNull] IApiAccessProperties properties, long id)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            return credential.DestroyDirectMessageAsync(properties, id, CancellationToken.None);
        }

        public static async Task<IApiResult<TwitterStatus>> DestroyDirectMessageAsync(
            [NotNull] this IOAuthCredential credential, IApiAccessProperties properties, long id,
            CancellationToken cancellationToken)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            };
            return await credential.PostAsync(properties, "direct_messages/destroy.json", param,
                ResultHandlers.ReadAsStatusAsync, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
