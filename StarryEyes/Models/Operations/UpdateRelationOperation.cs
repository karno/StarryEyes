using System;
using StarryEyes.Breezy.Api.Rest;
using StarryEyes.Breezy.Authorize;
using StarryEyes.Breezy.DataModel;

namespace StarryEyes.Models.Operations
{
    public class UpdateRelationOperation : OperationBase<TwitterUser>
    {
        private readonly AuthenticateInfo _info;
        private readonly TwitterUser _target;
        private readonly OperationType _operationType;

        public UpdateRelationOperation(AuthenticateInfo info, TwitterUser target, OperationType operationType)
        {
            _info = info;
            _target = target;
            _operationType = operationType;
        }

        protected override IObservable<TwitterUser> RunCore()
        {
            switch (_operationType)
            {
                case OperationType.Follow:
                    return _info.CreateFriendship(_target.Id);
                case OperationType.Unfollow:
                    return _info.DestroyFriendship(_target.Id);
                case OperationType.Block:
                    return _info.CreateBlock(_target.Id);
                case OperationType.ReportAsSpam:
                    return _info.ReportSpam(_target.Id);
                case OperationType.Unblock:
                    return _info.DestroyBlock(_target.Id);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum OperationType
    {
        Follow,
        Unfollow,
        Block,
        ReportAsSpam,
        Unblock
    }
}
