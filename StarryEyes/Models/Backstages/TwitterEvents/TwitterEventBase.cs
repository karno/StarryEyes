using System;
using System.Linq;
using System.Windows.Media;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Settings;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public abstract class TwitterEventBase : BackstageEventBase
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

        public bool IsLocalUserInvolved => Setting.Accounts.Ids.Contains(Source.Id) || Setting.Accounts.Ids.Contains(TargetUser.Id);

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

        public override Color Background
        {
            get { return MetroColors.Cyan; }
        }
    }
}
