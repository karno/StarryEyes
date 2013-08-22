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

        public static async Task<ICursorResult<IEnumerable<long>>> GetBlockingsIdsAsync(
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

        public static Task<TwitterUser> CreateBlockAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return CreateBlockCoreAsync(credential, userId, null);
        }

        public static Task<TwitterUser> CreateBlockAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return CreateBlockCoreAsync(credential, null, screenName);
        }

        private static async Task<TwitterUser> CreateBlockCoreAsync(
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

        public static Task<TwitterUser> DestroyBlockAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return DestroyBlockCoreAsync(credential, userId, null);
        }

        public static Task<TwitterUser> DestroyBlockAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return DestroyBlockCoreAsync(credential, null, screenName);
        }

        private static async Task<TwitterUser> DestroyBlockCoreAsync(
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

        public static Task<TwitterUser> ReportSpamAsync(
            this IOAuthCredential credential, long userId)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            return ReportSpamCoreAsync(credential, userId, null);
        }

        public static Task<TwitterUser> ReportSpamAsync(
            this IOAuthCredential credential, string screenName)
        {
            if (credential == null) throw new ArgumentNullException("credential");
            if (screenName == null) throw new ArgumentNullException("screenName");
            return ReportSpamCoreAsync(credential, null, screenName);
        }

        private static async Task<TwitterUser> ReportSpamCoreAsync(
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
