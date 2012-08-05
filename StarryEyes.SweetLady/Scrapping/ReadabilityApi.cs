using System;
using System.Reactive.Linq;
using Codeplex.OAuth;
using StarryEyes.SweetLady.Util;

namespace StarryEyes.Mystique.Scrapping
{
    /// <summary>
    /// F*********************************CK!!!!!!!!!!!!!!!!!!!!!!!!!!<para />
    /// Readability NEEDS OAUTH AUTHENTICATION!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// </summary>
    public class ReadabilityApi : ScrappingService
    {
        private string consumerKey;
        private string consumerSecret;
        private string userToken;
        private string userSecret;
        public ReadabilityApi(string consumerKey, string consumerSecret, string userToken, string userSecret)
            : base(userToken, userSecret)
        {
            this.consumerKey = consumerKey;
            this.consumerSecret = consumerSecret;
            this.userToken = userToken;
            this.userSecret = userSecret;
        }

        public static IObservable<TokenResponse<AccessToken>> GetTokenSecret(
            string consumerKey, string consumerSecret, string userId, string password)
        {
            return
                new OAuthAuthorizer(consumerKey, consumerSecret)
                .GetAccessToken("https://www.readability.com/api/rest/v1/oauth/access_token/", userId, password);
        }

        public override IObservable<bool> CheckAuth()
        {
            return Observable.Return(true);
        }

        public override IObservable<bool> Scrap(string url, string title = null, long? sourceTweetId = null)
        {
            var client = new OAuthClient(consumerKey, consumerSecret,
                new AccessToken(userToken, userSecret));
            client.Url = "https://www.readability.com/api/rest/v1/bookmarks";
            var param = new ParameterCollection();
            param.Add("url", HttpUtility.UrlEncode(url));
            client.Parameters = param;
            client.MethodType = MethodType.Post;
            return client.GetResponseText()
                .Select(_ => true);
        }
    }
}
