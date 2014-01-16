using System;
using System.Windows.Media;
using StarryEyes.Models.Accounting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public class PostLimitedEvent : BackstageEventBase
    {
        private readonly TwitterAccount _account;
        private readonly DateTime _releaseTime;

        public PostLimitedEvent(TwitterAccount account, DateTime releaseTime)
        {
            _account = account;
            _releaseTime = releaseTime;
        }

        public override string Title
        {
            get { return "POST LIMITED"; }
        }

        public override string Detail
        {
            get { return "POST規制されました。予想解除時刻は " + _releaseTime.ToString("HH\\:mm\\:ss") + " です。"; }
        }

        public override Color Background
        {
            get { return MetroColors.Indigo; }
        }
    }
}
