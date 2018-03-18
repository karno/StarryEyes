using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents
{
    internal sealed class InternalErrorEvent : BackstageEventBase
    {
        private readonly string _description;

        public InternalErrorEvent(string description)
        {
            _description = description;
        }

        public override string Title => "ERROR";

        public override string Detail => _description;

        public override System.Windows.Media.Color Background => MetroColors.Red;
    }
}