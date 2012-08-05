using System;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Operations
{
    public class RetweetOperation : OperationBase<TwitterStatus>
    {
        public RetweetOperation() { }
        public RetweetOperation(AuthenticateInfo auth, TwitterStatus status)
        {
            this.AuthInfo = auth;
            this.TargetId = status.Id;
            this.DescriptionText = status.ToString();
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        private AuthenticateInfo _originalAuthInfo { get; set; }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return AuthInfo.Retweet(TargetId)
                .Catch((Exception ex) =>
                {
                    /* fallback logic
                    return
                        GetExceptionDetail(ex)
                        .Select(s =>
                        {
                            long? fallbackId;
                            AuthenticateInfo fallbackAccount;
                            if (s.Contains("Wow, that's a lot of Twittering! You have reached your limit of updates for the day.") &&
                                (fallbackId = Setting.GetRelatedInfo(this.AuthInfo).FallbackNext).HasValue &&
                                (fallbackAccount = Setting.Accounts.Where(i => i.Id == fallbackId.Value).FirstOrDefault()) != null &&
                                (_originalAuthInfo == null || _originalAuthInfo.Id != fallbackAccount.Id))
                            {
                                // Post limit, go fallback
                                ShowToast("@" + this.AuthInfo.UnreliableScreenName + " is over daily status update limit.",
                                    "FALLBACK");
                                if (this._originalAuthInfo != null)
                                    this._originalAuthInfo = AuthInfo;
                                this.AuthInfo = fallbackAccount;
                                Twittaholic.UnlockStatic();
                                OperationQueueRunner.Dispatch(this);
                                return new Unit();
                            }
                            else
                            {
                                ShowToast(s, "RETWEET ERROR");
                                throw ex;
                            }
                        });
                    */
                    return Observable.Throw<TwitterStatus>(ex);
                });
        }
    }
}
