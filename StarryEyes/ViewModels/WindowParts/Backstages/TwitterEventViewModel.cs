using StarryEyes.Models.Backstages.TwitterEvents;

namespace StarryEyes.ViewModels.WindowParts.Backstages
{
    public class TwitterEventViewModel : BackstageEventViewModel
    {
        public TwitterEventViewModel(TwitterEventBase tev)
            : base(tev)
        {
        }

        public TwitterEventBase TwitterEvent
        {
            get { return this.SourceEvent as TwitterEventBase; }
        }
    }
}