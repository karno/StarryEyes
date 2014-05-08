using StarryEyes.Globalization;

namespace StarryEyes.Models.Backstages.SystemEvents
{
    public class UpdateAvailableEvent : SystemEventBase
    {
        public override string Detail
        {
            get { return BackstageResources.UpdateAvailable; }
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Notify; }
        }
    }
}
