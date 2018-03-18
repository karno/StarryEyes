using System;
using System.Linq;
using System.Windows.Media;
using Cadena.Data;
using StarryEyes.Settings;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.TwitterEvents
{
    public abstract class TwitterEventBase : BackstageEventBase
    {
        public DateTime CreatedAt { get; }

        public TwitterUser Source { get; }

        public TwitterUser TargetUser { get; }

        public TwitterStatus TargetStatus { get; }

        public bool IsLocalUserInvolved => Setting.Accounts.Ids.Contains(Source.Id) ||
                                           Setting.Accounts.Ids.Contains(TargetUser.Id);

        public TwitterEventBase(TwitterUser source, TwitterUser target)
        {
            CreatedAt = DateTime.Now;
            Source = source;
            TargetUser = target;
        }

        public TwitterEventBase(TwitterUser source, TwitterUser target, TwitterStatus targetStatus)
            : this(source, target)
        {
            TargetStatus = targetStatus;
        }

        public override Color Background => MetroColors.Cyan;
    }
}