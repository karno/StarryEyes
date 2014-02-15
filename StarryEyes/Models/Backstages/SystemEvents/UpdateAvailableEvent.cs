
namespace StarryEyes.Models.Backstages.SystemEvents
{
    public class UpdateAvailableEvent : SystemEventBase
    {
        public override string Detail
        {
            get { return "新しいバージョンのKrileが利用可能です。次回起動時に更新されます。"; }
        }

        public override SystemEventKind Kind
        {
            get { return SystemEventKind.Notify; }
        }
    }
}
