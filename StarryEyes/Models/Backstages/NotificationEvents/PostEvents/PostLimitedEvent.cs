using System;
using System.Windows.Media;
using StarryEyes.Globalization;
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
            get
            {
                return BackstageResources.PostLimitFormat.SafeFormat(
                    "@" + _account.UnreliableScreenName,
                    _releaseTime.ToString("HH\\:mm\\:ss"));
            }
        }

        public override Color Background
        {
            get { return MetroColors.Indigo; }
        }
    }
}
