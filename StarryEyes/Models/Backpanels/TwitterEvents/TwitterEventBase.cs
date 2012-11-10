using System;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.TwitterEvents
{
    public abstract class TwitterEventBase : BackpanelEventBase
    {
        private readonly DateTime _createdAt;
        public DateTime CreatedAt
        {
            get { return _createdAt; }
        } 


        private readonly TwitterUser _source;
        public TwitterUser Source
        {
            get { return _source; }
        }

        private readonly TwitterUser _targetUser;
        public TwitterUser TargetUser
        {
            get { return _targetUser; }
        }

        private readonly TwitterStatus _targetStatus;
        public TwitterStatus TargetStatus
        {
            get { return _targetStatus; }
        } 

        public TwitterEventBase(TwitterUser source, TwitterUser target)
        {
            this._createdAt = DateTime.Now;
            this._source = source;
            this._targetUser = target;
        }

        public TwitterEventBase(TwitterUser source, TwitterStatus target)
            : this(source, target.User)
        {
            this._targetStatus = target;
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Blue; }
        }
    }
}
