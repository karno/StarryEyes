using System;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Settings;
using System.Net;
using StarryEyes.Models.Store;

namespace StarryEyes.Models.Operations
{
    public class RetweetOperation : OperationBase<TwitterStatus>
    {
        public RetweetOperation() { }
        public RetweetOperation(AuthenticateInfo auth, TwitterStatus status,bool add)
        {
            this.AuthInfo = auth;
            this.TargetId = status.Id;
            this.DescriptionText = status.ToString();
            this.IsRetweet = add;
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        private AuthenticateInfo _originalAuthInfo { get; set; }

        public bool IsRetweet { get; set; }

        protected override IObservable<TwitterStatus> RunCore()
        {
            if (IsRetweet)
            {
                return AuthInfo.Retweet(TargetId)
                    .Catch((Exception ex) =>
                    {
                        return
                            GetExceptionDetail(ex)
                            .SelectMany(s =>
                            {
                                AccountSetting cas;
                                AccountSetting fallbackAccount;
                                if (s.Contains("Wow, that's a lot of Twittering! You have reached your limit of updates for the day.") &&
                                    (cas = AccountsStore.GetAccountSetting(this.AuthInfo.Id)) != null &&
                                    (fallbackAccount = AccountsStore.GetAccountSetting(cas.FallbackNext)) != null)
                                {
                                    // Post limit, go fallback
                                    if (this._originalAuthInfo != null)
                                        this._originalAuthInfo = AuthInfo;
                                    this.AuthInfo = fallbackAccount.AuthenticateInfo;
                                    return this.Run(OperationPriority.High);
                                }
                                else
                                {
                                    return Observable.Throw<TwitterStatus>(ex);
                                }
                            });
                    });
            }
            else
            {
                return AuthInfo.GetMyRetweetId(TargetId)
                    .Do(id =>
                    {
                        if (id == 0)
                            throw new WebException("Your retweet is not existed.");
                    })
                    .SelectMany(id => AuthInfo.Destroy(id));
            }
        }
    }
}
