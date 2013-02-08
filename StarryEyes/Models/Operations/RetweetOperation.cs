using System;
using System.Net;
using System.Reactive.Linq;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Stores;
using StarryEyes.Settings;

namespace StarryEyes.Models.Operations
{
    public class RetweetOperation : OperationBase<TwitterStatus>
    {
        private AuthenticateInfo _originalAuthInfo;

        public RetweetOperation()
        {
        }

        public RetweetOperation(AuthenticateInfo auth, TwitterStatus status, bool add)
        {
            AuthInfo = auth;
            TargetId = status.Id;
            DescriptionText = status.ToString();
            IsRetweet = add;
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        public bool IsRetweet { get; set; }

        protected override IObservable<TwitterStatus> RunCore()
        {
            if (IsRetweet)
            {
                return AuthInfo.Retweet(TargetId)
                               .Catch((Exception ex) =>
                                      GetExceptionDetail(ex)
                                          .SelectMany(s =>
                                          {
                                              AccountSetting cas;
                                              AccountSetting fallbackAccount;
                                              if (
                                                  s.Contains(
                                                      "Wow, that's a lot of Twittering! You have reached your limit of updates for the day.") &&
                                                  (cas = AccountsStore.GetAccountSetting(AuthInfo.Id)) != null &&
                                                  (fallbackAccount = AccountsStore.GetAccountSetting(cas.FallbackNext)) !=
                                                  null)
                                              {
                                                  // Post limit, go fallback
                                                  if (_originalAuthInfo != null)
                                                      _originalAuthInfo = AuthInfo;
                                                  AuthInfo = fallbackAccount.AuthenticateInfo;
                                                  return Run(OperationPriority.High);
                                              }
                                              return Observable.Throw<TwitterStatus>(new RetweetFailedException(s, ex));
                                          }));
            }
            return AuthInfo.GetMyRetweetId(TargetId)
                           .Do(id =>
                           {
                               if (id == 0)
                                   throw new WebException("Your retweet is not existed.");
                           })
                           .SelectMany(id => AuthInfo.Destroy(id));
        }
    }


    [Serializable]
    public class RetweetFailedException : Exception
    {
        public RetweetFailedException(string message, Exception inner) : base(message, inner) { }
        protected RetweetFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}