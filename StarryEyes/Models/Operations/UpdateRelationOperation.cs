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
        private readonly RelationKind _relationKind;

        public AuthenticateInfo Info
        {
            get { return this._info; }
        }

        public TwitterUser Target
        {
            get { return this._target; }
        }

        public RelationKind RelationKind
        {
            get { return this._relationKind; }
        }

        public UpdateRelationOperation(AuthenticateInfo info, TwitterUser target, RelationKind relationKind)
        {
            this._info = info;
            this._target = target;
            this._relationKind = relationKind;
        }

        protected override IObservable<TwitterUser> RunCore()
        {
            switch (this.RelationKind)
            {
                case RelationKind.Follow:
                    return this.Info.CreateFriendship(this.Target.Id);
                case RelationKind.Unfollow:
                    return this.Info.DestroyFriendship(this.Target.Id);
                case RelationKind.Block:
                    return this.Info.CreateBlock(this.Target.Id);
                case RelationKind.ReportAsSpam:
                    return this.Info.ReportSpam(this.Target.Id);
                case RelationKind.Unblock:
                    return this.Info.DestroyBlock(this.Target.Id);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum RelationKind
    {
        Follow,
        Unfollow,
        Block,
        ReportAsSpam,
        Unblock
    }
}
