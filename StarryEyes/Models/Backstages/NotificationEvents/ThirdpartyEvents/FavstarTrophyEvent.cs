using Cadena.Data;
using StarryEyes.Views;

namespace StarryEyes.Models.Backstages.NotificationEvents.ThirdpartyEvents
{
    public class TrophyScceededEvent : BackstageEventBase
    {
        public TwitterStatus Status { get; set; }

        public TrophyScceededEvent(TwitterStatus status)
        {
            Status = status;
        }

        public override string Title => "TROPHY";

        public override string Detail => "Successfully picked it as tweet of the day: " + Status;

        public override System.Windows.Media.Color Background => MetroColors.Pink;
    }

    public class TrophyFailedEvent : BackstageEventBase
    {
        public TwitterStatus Status { get; set; }
        public string Response { get; set; }

        public TrophyFailedEvent(TwitterStatus status, string response)
        {
            Status = status;
            Response = response;
        }

        public override string Title => "TROPHY FAILED";

        public override string Detail => Response;

        public override System.Windows.Media.Color Background => MetroColors.Red;
    }
}