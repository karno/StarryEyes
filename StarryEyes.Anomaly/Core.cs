using System.Security.Cryptography;
using AsyncOAuth;

namespace StarryEyes.Anomaly
{
    public static class Core
    {
        internal static readonly string DefaultConsumerKey = "9gdjLcTP01nMaO1u4xoKKw";

        internal static readonly string DefaultConsumerSecret = "WkhKRPIC4bmkLn0dvaSENPv2PVPIw4idiB3f1ppes";

        public static void Initialize()
        {
            // init oauth util
            OAuthUtility.ComputeHash = (key, buffer) =>
            {
                using (var hmac = new HMACSHA1(key))
                {
                    return hmac.ComputeHash(buffer);
                }
            };
        }
    }
}
