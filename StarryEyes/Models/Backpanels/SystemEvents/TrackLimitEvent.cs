using StarryEyes.Breezy.Authorize;
using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.SystemEvents
{
    public sealed class TrackLimitEvent : BackpanelEventBase
    {
        private readonly AuthenticateInfo _info;
        private readonly int _drop;

        public TrackLimitEvent(AuthenticateInfo relatedInfo, int drop)
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
            get { return _drop + " 件のツイートを受信できませんでした。(タイムラインが速すぎます。トラック設定を見直すことをお勧めします。)"; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Brown; }
        }
    }
}
