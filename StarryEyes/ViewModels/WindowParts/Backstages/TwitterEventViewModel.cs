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
            if (ev?.Source == null) return;
            BackstageModel.RaiseCloseBackstage();
            SearchFlipModel.RequestSearch(ev.Source.ScreenName, SearchMode.UserScreenName);
        }

        public void OpenEventTargetStatus()
        {
            var ev = TwitterEvent;
            if (ev?.TargetStatus == null) return;
            BackstageModel.RaiseCloseBackstage();
            SearchFlipModel.RequestSearch("?from conv:\"" + ev.TargetStatus.Id + "\"", SearchMode.Local);
        }

        public void OpenEventDetail()
        {
            var ev = TwitterEvent;

            if (ev?.TargetStatus != null)
                OpenEventTargetStatus();

            if (ev?.Source != null)
                OpenEventSourceUserProfile();
        }
    }
}