using System;
using System.Windows.Input;
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

        public Uri HeaderImageUri
        {
            get { return User.ProfileBackgroundImageUri; }
        }

        public bool IsProtected { get { return User.IsProtected; } }

        public bool IsVerified { get { return User.IsVerified; } }

        public bool IsTranslator { get { return User.IsTranslator; } }

        public long StatusesCount { get { return User.StatusesCount; } }

        public long FollowingsCount { get { return User.FriendsCount; } }

        public long FollowersCount { get { return User.FollowersCount; } }

        public long FavoritesCount { get { return User.FavoritesCount; } }

        public long ListedCount { get { return User.ListedCount; } }

        public DateTime CreatedAt { get { return User.CreatedAt; } }

        public string Name
        {
            get { return User.Name; }
        }

        public string ScreenName
        {
            get { return User.ScreenName; }
        }

        public string Bio
        {
            get { return User.Description; }
        }

        public string Location
        {
            get { return User.Location; }
        }

        public string Web
        {
            get { return User.Url; }
        }

        public bool IsWellformed
        {
            get { return Uri.IsWellFormedUriString(User.Url, UriKind.Absolute); }
        }

        public Uri WebUri
        {
            get
            {
                Uri uri;
                if (Uri.TryCreate(User.Url, UriKind.Absolute, out uri))
                {
                    return uri;
                }
                return null;
            }
        }

        public void OpenUserWeb()
        {
            if (!String.IsNullOrWhiteSpace(Web))
            {
                BrowserHelper.Open(Web);
            }
        }

        public void OpenUserDetail()
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                OpenUserDetailOnTwitter();
            }
            else
            {
                OpenUserDetailLocal();
            }
        }

        public void OpenUserDetailLocal()
        {
            SearchFlipModel.RequestSearch(this.ScreenName, SearchMode.UserScreenName);
        }

        public void OpenUserDetailOnTwitter()
        {
            BrowserHelper.Open("http://twitter.com/" + this.ScreenName);
        }
    }
}