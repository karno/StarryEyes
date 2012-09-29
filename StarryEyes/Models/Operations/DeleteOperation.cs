using System;
using StarryEyes.Moon.Api.Rest;
using StarryEyes.Moon.Authorize;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.Models.Operations
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