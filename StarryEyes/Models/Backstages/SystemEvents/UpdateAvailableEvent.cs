
namespace StarryEyes.Models.Backstages.SystemEvents
{
    public class UpdateAvailableEvent : SystemEventBase
    {
        public override string Detail
        {
            get { return "updated version of Krile is now available. Please restart."; }
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Notify; }
        }
    }
}
