using System;
using StarryEyes.SweetLady.Api.Rest;
using StarryEyes.SweetLady.Authorize;
using StarryEyes.SweetLady.DataModel;

namespace StarryEyes.Mystique.Models.Operations
{
    public class DeleteOperation : OperationBase<TwitterStatus>
    {
        public AuthenticateInfo AuthInfo { get; set; }

        public long TargetId { get; set; }

        public string DescriptionText { get; set; }

        public bool IsDirectMessage { get; set; }

        private Action cancelHandler;
        private Action completeHandler;

        public DeleteOperation() { }
        public DeleteOperation(AuthenticateInfo info, TwitterStatus status, Action cancel, Action completed)
        {
            AuthInfo = info;
            TargetId = status.Id;
            DescriptionText = status.ToString();
            IsDirectMessage = status.StatusType == StatusType.DirectMessage;
            cancelHandler = cancel;
            completeHandler = completed;
        }

        protected override IObservable<TwitterStatus> RunCore()
        {
            return (!IsDirectMessage ? AuthInfo.Destroy(TargetId) : AuthInfo.DestroyDirectMessage(TargetId));
        }
    }
}