using StarryEyes.Models;
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

        public void OpenEventSourceUserProfile()
        {
            var ev = TwitterEvent;
            if (ev == null || ev.Source == null) return;
            BackstageModel.RaiseCloseBackstage();
            SearchFlipModel.RequestSearch(ev.Source.ScreenName, SearchMode.UserScreenName);
        }
    }
}