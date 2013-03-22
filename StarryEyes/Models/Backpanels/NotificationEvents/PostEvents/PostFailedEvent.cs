using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.NotificationEvents.PostEvents
{
    public sealed class PostFailedEvent : BackpanelEventBase
    {
        private readonly TweetInputInfo _tweetInputInfo;

        private readonly string _reason;

        public PostFailedEvent(TweetInputInfo info, string reason)
        {
            this._tweetInputInfo = info;
            this._reason = reason;
        }

        public override string Title
        {
            get { return "FAILED"; }
        }

        public override string Detail
        {
            get { return _reason + " - " + _tweetInputInfo.Text; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Red; }
        }
    }
}
