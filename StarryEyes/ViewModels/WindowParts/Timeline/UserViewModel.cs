using Livet;
using StarryEyes.Models;
using StarryEyes.Moon.DataModel;

namespace StarryEyes.ViewModels.WindowParts.Timeline
{
    public class UserViewModel : ViewModel
    {
        public UserModel Model { get; private set; }
        public TwitterUser User { get { return Model.User; } }
        public UserViewModel(TwitterUser user)
        {
            this.Model = UserModel.Get(user);
        }

        public string ScreenName
        {
            get { return this.User.ScreenName; }
        }
    }
}
