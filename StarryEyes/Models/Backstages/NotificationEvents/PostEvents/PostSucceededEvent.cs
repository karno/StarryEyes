using StarryEyes.Models.Inputting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class PostSucceededEvent : BackstageEventBase
    {
        public PostSucceededEvent(InputData data)
        {
            Detail = data.Text;
        }

        public override string Title => "SENT";

        public override string Detail { get; }

        public override System.Windows.Media.Color Background => MetroColors.Cyan;
    }
}