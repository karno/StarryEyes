using System;
using System.Net;
using System.Reactive.Linq;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Mystique.Models.Store;

namespace StarryEyes.Mystique.Models.Operations
{
    public class DirectMessageOperation : OperationBase<TwitterStatus>
    {
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
