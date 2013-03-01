using StarryEyes.Breezy.DataModel;
using StarryEyes.ViewModels.WindowParts.Flips.UserFlip;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class UserInformationFlipViewModel : PartialFlipViewModelBase
    {
        private UserViewModel _viewModel;

        private UserTimelineViewModel _timelineViewModel;

        public UserViewModel User
        {
            get { return _viewModel; }
        }

        public UserTimelineViewModel Timeline
        {
            get { return _timelineViewModel; }
            set { _timelineViewModel = value; }
        }

        public void SetUser(TwitterUser user)
        {
            if (user == null) return;
            _viewModel = new UserViewModel(user);
            _timelineViewModel = new UserTimelineViewModel(user);
        }
    }
}
