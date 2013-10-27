using System.Windows.Media;
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
            this._info = relatedInfo;
            this._drop = drop;
        }

        public override string Title
        {
            get { return "TRACK LIMIT"; }
        }

        public override string Detail
        {
            get { return _drop + " 件のツイートを受信できませんでした。 " + _info.UnreliableScreenName + " のタイムラインが速すぎます。"; }
        }

        public override Color Background
        {
            get { return MetroColors.Mauve; }
        }
    }
}