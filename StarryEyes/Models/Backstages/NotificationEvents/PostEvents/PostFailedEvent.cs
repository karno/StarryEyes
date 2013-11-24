using StarryEyes.Models.Inputting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class PostFailedEvent : BackstageEventBase
    {
        private readonly string _post;

        private readonly string _reason;

        public PostFailedEvent(TweetInputInfo info, string reason)
        {
            this._post = info.Text;
            this._reason = reason;
        }

        public PostFailedEvent(InputData data, string reason)
        {
            this._post = data.Text;
            this._reason = reason;
        }


        public override string Title
        {
            get { return "FAILED"; }
        }

        public override string Detail
        {
            get { return _reason + " - " + this._post; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Red; }
        }
    }
}
