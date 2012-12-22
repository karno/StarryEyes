using System;
using Livet;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models;

namespace StarryEyes.ViewModels.WindowParts.Timelines
{
    public class UserViewModel : ViewModel
    {
        public UserViewModel(TwitterUser user)
        {
            Model = UserModel.Get(user);
        }

        public UserModel Model { get; private set; }

        public TwitterUser User
        {
            get { return Model.User; }
        }

        public Uri ProfileImageUri
        {
            get { return User.ProfileImageUri; }
        }

        public string ScreenName
        {
            get { return User.ScreenName; }
        }
    }
}