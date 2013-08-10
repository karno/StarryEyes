using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;

namespace StarryEyes.Anomaly.TwitterApi.Rest
{
    public static class Blockings
    {
        #region Blocking ids

        public static async Task<ICursorResult<IEnumerable<long>>> GetBlockingsIds(
            this IOAuthCredential credential, long cursor = -1)
        {
            var param = new Dictionary<string, object>
            {
                {"cursor", cursor},
            }.ParametalizeForGet();
            var client = credential.CreateOAuthClient();
            var response = await client.GetAsync(new ApiAccess("blocks/ids.json", param));
            return await response.ReadAsCursoredIdsAsync();
        }

        #endregion

        #region Block

        public static Task<TwitterUser> CreateBlock(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return CreateBlockCore(credential, userId, null);
        }

        public static Task<TwitterUser> CreateBlock(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return CreateBlockCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> CreateBlockCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("blocks/create.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Unblock

        public static Task<TwitterUser> DestroyBlock(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return DestroyBlockCore(credential, userId, null);
        }

        public static Task<TwitterUser> DestroyBlock(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return DestroyBlockCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> DestroyBlockCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("blocks/destroy.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion

        #region Report Spam

        public static Task<TwitterUser> ReportSpam(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ReportSpamCore(credential, userId, null);
        }

        public static Task<TwitterUser> ReportSpam(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return ReportSpamCore(credential, null, screenName);
        }

        private static async Task<TwitterUser> ReportSpamCore(
            IOAuthCredential credential, long? userId, string screenName)
        {
            var param = new Dictionary<string, object>
            {
                {"user_id", userId},
                {"screen_name", screenName},
            }.ParametalizeForPost();
            var client = credential.CreateOAuthClient();
            var response = await client.PostAsync(new ApiAccess("users/report_spam.json"), param);
            return await response.ReadAsUserAsync();
        }

        #endregion
    }
}
