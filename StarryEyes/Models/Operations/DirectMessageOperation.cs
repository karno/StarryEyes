using System;
using System.Net;
using System.Reactive.Linq;
using StarryEyes.Models.Store;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Models.Operations
{
    public class DirectMessageOperation : OperationBase<TwitterStatus>
    {
        public DirectMessageOperation() { }

        public DirectMessageOperation(AuthenticateInfo info, TwitterUser target, string text)
        {
            this.AuthInfo = info;
            this.TargetUserId = target.Id;
            this.Text = text;
        }

        public AuthenticateInfo AuthInfo { get; set; }

        public string Text { get; set; }

        public long TargetUserId { get; set; }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return AuthInfo.SendDirectMessage(Text, TargetUserId)
                .ObserveOnDispatcher()
                // .Do(s => ShowToast(s.ToString(), "DM SENT"))
                .Do(s => StatusStore.Store(s))
                .Catch((Exception ex) =>
                {
                    return GetExceptionDetail(ex)
                        .Select(s => Observable.Throw<TwitterStatus>(new WebException(s)))
                        .SelectMany(_ => _);
                });
        }
    }
}
