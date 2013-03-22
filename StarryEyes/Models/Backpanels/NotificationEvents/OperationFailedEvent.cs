using StarryEyes.Views;

namespace StarryEyes.Models.Backpanels.NotificationEvents
{
    public class OperationFailedEvent : BackpanelEventBase
    {
        private readonly string _description;

        public OperationFailedEvent(string description)
        {
            this._description = description;
        }

        public override string Title
        {
            get { return "FAILED"; }
        }

        public override string Detail
        {
            get { return _description; }
        }

        public override System.Windows.Media.Color Background
        {
            get { return MetroColors.Red; }
        }
    }
}
