using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Operations
{
    public class DeleteOperation : OperationBase<TwitterStatus>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        public bool IsDirectMessage { get; set; }

        public DeleteOperation() { }
        public DeleteOperation(AuthenticateInfo info, TwitterStatus status)
        {
            AuthInfo = info;
            TargetId = status.Id;
            DescriptionText = status.ToString();
            IsDirectMessage = status.StatusType == StatusType.DirectMessage;
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return (!IsDirectMessage ? AuthInfo.Destroy(TargetId) : AuthInfo.DestroyDirectMessage(TargetId));
        }
    }
}