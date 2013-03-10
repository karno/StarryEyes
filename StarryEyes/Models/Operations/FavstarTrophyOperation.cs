using System;
using System.Reactive;
using System.Reactive.Linq;
using Codeplex.OAuth;
using StarryEyes.Breezy.Api;
using StarryEyes.Breezy.Api.Parsing;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Breezy.Net;
using StarryEyes.Models.Backpanels.ThirdpartyEvents;
using StarryEyes.Settings;

namespace StarryEyes.Models.Operations
{
    public class FavstarTrophyOperation : OperationBase<Unit>
    {
        public AuthenticateInfo AuthInfo { get; set; }
        public TwitterStatus TargetStatus { get; set; }

        public FavstarTrophyOperation(AuthenticateInfo info, TwitterStatus status)
        {
            AuthInfo = info;
            TargetStatus = status;
        }


        protected override IObservable<Unit> RunCore()
        {
            var client = new OAuthEchoClient(
                AuthInfo.AccessToken,
                AuthInfo.OverridedConsumerKey ?? ApiEndpoint.DefaultConsumerKey,
                AuthInfo.OverridedConsumerSecret ?? ApiEndpoint.DefaultConsumerSecret);
            client.ApplyBeforeRequest = req =>
            {
                req.UserAgent = ApiEndpoint.UserAgent;
                req.Headers.Add("X-Twitter-User-ID", AuthInfo.Id.ToString());
                req.Headers.Add("X-Favstar-API-Key", Setting.FavstarApiKey.Value);
            };
            client.Url = "http://favstar.fm/tweets/" + TargetStatus.Id + "/record_tweet_of_the_day";
            client.MethodType = MethodType.Post;
            return client.GetResponseText()
                         .DeserializeJson<FavstarJsonResponse>()
                         .Select(response =>
                         {
                             BackpanelModel.RegisterEvent(new TrophyScceededEvent(TargetStatus));
                             return Unit.Default;
                         })
                         .Catch((Exception ex) =>
                         {
                             BackpanelModel.RegisterEvent(new TrophyFailedEvent(TargetStatus, ex.Message));
                             return Observable.Empty<Unit>();
                         });
        }
    }

    public class FavstarJsonResponse
    {
        public string message { get; set; }
    }
}
