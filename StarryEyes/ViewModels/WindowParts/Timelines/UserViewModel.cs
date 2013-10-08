using System;
using System.Windows.Input;
using Livet;
using StarryEyes.Anomaly.TwitterApi.DataModels;
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
            get
            {
                if (User.ProfileImageUri == null) return null;
                var uri = User.ProfileImageUri.OriginalString;
                if (uri.EndsWith("normal.png"))
                {
                    uri = uri.Substring(0, uri.Length - 10) + "bigger.png";
                }
                try
                {
                    return new Uri(uri);
                }
                catch (UriFormatException)
                {
                    return User.ProfileImageUri;
                }
            }
        }

        public Uri BackgroundImageUri
        {
            get { return User.ProfileBackgroundImageUri; }
        }

        public Uri BannerImageUri
        {
            get
            {
                if (User.ProfileBannerUri == null) return null;
                var uri = User.ProfileBannerUri.OriginalString;
                if (!uri.EndsWith("/"))
                {
                    uri += "/";
                }
                return new Uri(uri + "web");
            }
        }

        public Uri UserSubImageUri
        {
            get { return BannerImageUri ?? BackgroundImageUri; }
        }

        public bool IsProtected { get { return User.IsProtected; } }

        public bool IsVerified { get { return User.IsVerified; } }

        public bool IsTranslator { get { return User.IsTranslator; } }

        public long StatusesCount { get { return User.StatusesCount; } }

        public long FollowingsCount { get { return User.FollowingsCount; } }

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
            get { return User.GetEntityAidedUrl(); }
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

        public void OpenUserFavstar()
        {
            BrowserHelper.Open(User.FavstarUserPermalink);
        }

        public void OpenUserTwilog()
        {
            BrowserHelper.Open(User.TwilogUserPermalink);
        }

    }
}