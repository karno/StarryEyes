using System;
using System.Windows.Input;
using Livet;
using Livet.EventListeners;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Anomaly.Utils;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.Timelines.Statuses
{
    public class UserViewModel : ViewModel
    {
        public UserViewModel(TwitterUser user)
        {
            this.Model = UserModel.Get(user);
            this.CompositeDisposable.Add(
                new EventListener<Action<TimelineIconResolution>>(
                    h => Setting.IconResolution.ValueChanged += h,
                    h => Setting.IconResolution.ValueChanged -= h,
                    _ => this.RaisePropertyChanged(() => ProfileImageUriOptimized)));
        }

        public UserModel Model { get; private set; }

        public TwitterUser User
        {
            get { return this.Model.User; }
        }

        public Uri ProfileImageUriOptimized
        {
            get
            {
                if (!Setting.IsLoaded)
                {
                    return ProfileImageUriOriginal;
                }
                switch (Setting.IconResolution.Value)
                {
                    case TimelineIconResolution.Original:
                        return this.ProfileImageUriOriginal;
                    case TimelineIconResolution.High:
                        return this.ProfileImageUriLarge;
                    case TimelineIconResolution.Optimized:
                        return this.ProfileImageUriNormal;
                    case TimelineIconResolution.Low:
                        return this.ProfileImageUriMini;
                    case TimelineIconResolution.None:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return this.User.ProfileImageUri.ChangeImageSize(ImageSize.Original);
            }
        }

        public Uri ProfileImageUriOriginal
        {
            get { return this.User.ProfileImageUri.ChangeImageSize(ImageSize.Original); }
        }

        public Uri ProfileImageUriLarge
        {
            get { return this.User.ProfileImageUri.ChangeImageSize(ImageSize.Bigger); }
        }

        public Uri ProfileImageUriNormal
        {
            get { return this.User.ProfileImageUri.ChangeImageSize(ImageSize.Normal); }
        }

        public Uri ProfileImageUriMini
        {
            get { return this.User.ProfileImageUri.ChangeImageSize(ImageSize.Mini); }
        }

        public Uri BackgroundImageUri
        {
            get { return this.User.ProfileBackgroundImageUri; }
        }

        public Uri BannerImageUri
        {
            get
            {
                if (this.User.ProfileBannerUri == null) return null;
                var uri = this.User.ProfileBannerUri.OriginalString;
                if (!uri.EndsWith("/"))
                {
                    uri += "/";
                }
                return new Uri(uri + "web");
            }
        }

        public Uri UserSubImageUri
        {
            get { return this.BannerImageUri ?? this.BackgroundImageUri; }
        }

        public bool IsProtected { get { return this.User.IsProtected; } }

        public bool IsVerified { get { return this.User.IsVerified; } }

        public bool IsTranslator { get { return this.User.IsTranslator; } }

        public long StatusesCount { get { return this.User.StatusesCount; } }

        public long FollowingsCount { get { return this.User.FollowingsCount; } }

        public long FollowersCount { get { return this.User.FollowersCount; } }

        public long FavoritesCount { get { return this.User.FavoritesCount; } }

        public long ListedCount { get { return this.User.ListedCount; } }

        public DateTime CreatedAt { get { return this.User.CreatedAt; } }

        public string Name
        {
            get { return this.User.Name; }
        }

        public string ScreenName
        {
            get { return this.User.ScreenName; }
        }

        public string Bio
        {
            get { return this.User.Description; }
        }

        public string Location
        {
            get { return this.User.Location; }
        }

        public string Web
        {
            get { return this.User.GetEntityAidedUrl(); }
        }

        public bool IsWellformed
        {
            get { return Uri.IsWellFormedUriString(this.User.Url, UriKind.Absolute); }
        }

        public Uri WebUri
        {
            get
            {
                Uri uri;
                if (Uri.TryCreate(this.User.Url, UriKind.Absolute, out uri))
                {
                    return uri;
                }
                return null;
            }
        }

        public void OpenUserWeb()
        {
            if (!String.IsNullOrWhiteSpace(this.Web))
            {
                BrowserHelper.Open(this.Web);
            }
        }

        public void OpenUserDetail()
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                this.OpenUserDetailOnTwitter();
            }
            else
            {
                this.OpenUserDetailLocal();
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
            BrowserHelper.Open(this.User.FavstarUserPermalink);
        }

        public void OpenUserTwilog()
        {
            BrowserHelper.Open(this.User.TwilogUserPermalink);
        }

    }
}