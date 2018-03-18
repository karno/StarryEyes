using StarryEyes.Models.Inputting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class PostFailedEvent : BackstageEventBase
    {
        private readonly string _post;

        private readonly string _reason;

        public PostFailedEvent(InputData data, string reason)
        {
            _post = data.Text;
            _reason = reason;
        }


        public override string Title => "FAILED";

        public override string Detail => _reason + " - " + _post;

        public override System.Windows.Media.Color Background => MetroColors.Red;
    }
}