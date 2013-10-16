using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class DirectMessages
    {
        public static async Task<IEnumerable<TwitterStatus>> GetDirectMessagesAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("direct_messages.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<IEnumerable<TwitterStatus>> GetSentDirectMessagesAsync(
            this IOAuthCredential credential,
            int? count = null, long? sinceId = null, long? maxId = null, int? page = null)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"count", count},
                {"since_id", sinceId},
                {"max_id", maxId},
                {"page", page},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("direct_messages/sent.json", param));
            return await response.ReadAsStatusCollectionAsync();
        }

        public static async Task<TwitterStatus> ShowDirectMessageAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("direct_messages/show.json", param));
            return await response.ReadAsStatusAsync();
        }

        public static Task<TwitterStatus> SendDirectMessageAsync(
            this IOAuthCredential credential, long recipientUserId, string text)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (text == null) throw new ArgumentNullException("text");
            return SendDirectMessageCoreAsync(credential, recipientUserId, null, text);
        }

        public static Task<TwitterStatus> SendDirectMessageAsync(
            this IOAuthCredential credential, string recipientUserScreenName, string text)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (recipientUserScreenName == null) throw new ArgumentNullException("recipientUserScreenName");
            if (text == null) throw new ArgumentNullException("text");
            return SendDirectMessageCoreAsync(credential, null, recipientUserScreenName, text);
        }

        private static async Task<TwitterStatus> SendDirectMessageCoreAsync(
            IOAuthCredential credential, long? recipientUserId, string recipientScreenName, string text)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", recipientUserId},
                {"screen_name", recipientScreenName},
                {"text", text}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("direct_messages/new.json"), param);
            return await response.ReadAsStatusAsync();
        }

        public static async Task<TwitterStatus> DestroyDirectMessageAsync(
            this IOAuthCredential credential, long id)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            var param = new Dictionary<string, object>
            {
                {"id", id}
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("direct_messages/destroy.json"), param);
            return await response.ReadAsStatusAsync();
        }
    }
}
