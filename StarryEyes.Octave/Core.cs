using System.Security.Cryptography;
using AsyncOAuth;

namespace StarryEyes.Octave
{
    public static class Core
    {
        public static void Initialize()
        {
            // init oauth util
            OAuthUtility.ComputeHash = (key, buffer) => { using (var hmac = new HMACSHA1(key)) { return hmac.ComputeHash(buffer); } };
        }
    }
}
