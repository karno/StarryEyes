using System;
using System.Windows.Input;
using Cadena.Data;
using Cadena.Util;
using JetBrains.Annotations;
using Livet;
using Livet.EventListeners;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.Timelines.Statuses
{
    public class UserViewModel : ViewModel
    {
        public UserViewModel([CanBeNull] TwitterUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            Model = UserModel.Get(user);
            CompositeDisposable.Add(
                new EventListener<Action<TimelineIconResolution>>(
                    h => Setting.IconResolution.ValueChanged += h,
                    h => Setting.IconResolution.ValueChanged -= h,
                    _ => RaisePropertyChanged(() => ProfileImageUriOptimized)));
        }

        public UserModel Model { get; }

        public TwitterUser User => Model.User;

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
                        return ProfileImageUriOriginal;
                    case TimelineIconResolution.High:
                        return ProfileImageUriLarge;
                    case TimelineIconResolution.Optimized:
                        return ProfileImageUriNormal;
                    case TimelineIconResolution.Low:
                        return ProfileImageUriMini;
                    case TimelineIconResolution.None:
                        return null;
                }
                return User.ProfileImageUri.ChangeImageSize(ImageSize.Original);
            }
        }

        public Uri ProfileImageUriOriginal => User.ProfileImageUri.ChangeImageSize(ImageSize.Original);

        public Uri ProfileImageUriLarge => User.ProfileImageUri.ChangeImageSize(ImageSize.Bigger);

        public Uri ProfileImageUriNormal => User.ProfileImageUri.ChangeImageSize(ImageSize.Normal);

        public Uri ProfileImageUriMini => User.ProfileImageUri.ChangeImageSize(ImageSize.Mini);

        public Uri BackgroundImageUri => User.ProfileBackgroundImageUri;

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

        public Uri UserSubImageUri => BannerImageUri ?? BackgroundImageUri;

        public bool IsProtected => User.IsProtected;

        public bool IsVerified => User.IsVerified;

        public bool IsTranslator => User.IsTranslator;

        public long StatusesCount => User.StatusesCount;

        public long FollowingsCount => User.FollowingsCount;

        public long FollowersCount => User.FollowersCount;

        public long FavoritesCount => User.FavoritesCount;

        public long ListedCount => User.ListedCount;

        public DateTime CreatedAt => User.CreatedAt;

        public string Name => User.Name;

        public string ScreenName => User.ScreenName;

        public string Bio => User.Description;

        public string Location => User.Location;

        public string Web => User.GetEntityAidedUrl();

        public bool IsWellformed => Uri.IsWellFormedUriString(User.Url, UriKind.Absolute);

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
            SearchFlipModel.RequestSearch(ScreenName, SearchMode.UserScreenName);
        }

        public void OpenUserDetailOnTwitter()
        {
            BrowserHelper.Open("http://twitter.com/" + ScreenName);
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