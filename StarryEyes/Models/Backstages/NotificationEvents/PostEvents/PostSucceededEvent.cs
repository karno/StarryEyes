using StarryEyes.Models.Inputting;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.PostEvents
{
    public sealed class PostSucceededEvent : BackstageEventBase
    {
        private readonly string _body;

        public PostSucceededEvent(InputData data)
        {
            this._body = data.Text;
        }

        public override string Title
        {
            get { return "SENT"; }
        }

        public override string Detail
        {
            get { return _body; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Cyan; }
        }
    }
}
