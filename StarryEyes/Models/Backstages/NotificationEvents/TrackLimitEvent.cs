using System.Windows.Media;
using StarryEyes.Globalization;
using StarryEyes.Models.Accounting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents
{
    public sealed class TrackLimitEvent : BackstageEventBase
    {
        private readonly TwitterAccount _info;
        private readonly int _drop;

        public TrackLimitEvent(TwitterAccount relatedInfo, int drop)
        {
            _info = relatedInfo;
            _drop = drop;
        }

        public override string Title => "TRACK LIMIT";

        public override string Detail => BackstageResources.TrackLimitFormat.SafeFormat(
            _drop, "@" + _info.UnreliableScreenName);

        public override Color Background => MetroColors.Mauve;
    }
}