using Livet;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.ViewModels.WindowParts.Timeline
{
    public class UserViewModel : ViewModel
    {
        public TwitterUser User { get; private set; }
        public UserViewModel(TwitterUser user)
        {
            this.User = user;
        }

        public string ScreenName
        {
            get { return this.User.ScreenName; }
        }
    }
}
