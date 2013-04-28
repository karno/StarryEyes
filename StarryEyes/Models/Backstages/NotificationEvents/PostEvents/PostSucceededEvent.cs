using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class PostSucceededEvent : BackstageEventBase
    {
        private readonly TweetInputInfo _tweetInputInfo;

        public PostSucceededEvent(TweetInputInfo info)
        {
            this._tweetInputInfo = info;
        }

        public override string Title
        {
            get { return "SENT"; }
        }

        public override string Detail
        {
            get { return _tweetInputInfo.Text; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Blue; }
        }
    }
}
