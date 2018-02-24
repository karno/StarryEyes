using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Cadena.Util;
using JetBrains.Annotations;
using Livet;
using Livet.Commands;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Globalization;
using StarryEyes.Globalization.WindowParts;
using StarryEyes.Models;
using StarryEyes.Models.Inputting;
using StarryEyes.Models.Stores;
using StarryEyes.Models.Timelines.SearchFlips;
using StarryEyes.Nightmare.Windows;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines.SearchFlips;
using StarryEyes.ViewModels.Timelines.Statuses;
using StarryEyes.Views.Messaging;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts.Flips.SearchFlips
{
    public class UserInfoViewModel : ViewModel
    {
        private readonly SearchFlipViewModel _parent;
        private string _screenName;
        private UserTimelineViewModel _statuses;
        private UserTimelineViewModel _favorites;
        private UserFollowingViewModel _following;
        private UserFollowersViewModel _followers;
        private bool _communicating = true;
        private UserViewModel _user;

        private UserDisplayKind _displayKind = UserDisplayKind.Statuses;

        public UserDisplayKind DisplayKind
        {
            get { return _displayKind; }
            set
            {
                if (_displayKind == value) return;
                _displayKind = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsVisibleStatuses);
                RaisePropertyChanged(() => IsVisibleFavorites);
                RaisePropertyChanged(() => IsVisibleFollowing);
                RaisePropertyChanged(() => IsVisibleFollowers);
            }
        }

        private ObservableCollection<RelationControlViewModel> _relationControls =
            new ObservableCollection<RelationControlViewModel>();

        public ObservableCollection<RelationControlViewModel> RelationControls
        {
            get { return _relationControls; }
            set { _relationControls = value; }
        }

        public SearchFlipViewModel Parent
        {
            get { return _parent; }
        }

        public string ScreenName
        {
            get { return _screenName; }
        }

        public bool Communicating
        {
            get { return _communicating; }
            set
            {
                _communicating = value;
                RaisePropertyChanged();
            }
        }

        public UserViewModel User
        {
            get { return _user; }
            set
            {
                _user = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsUserAvailable);
            }
        }

        public bool IsUserAvailable
        {
            get { return User != null; }
        }

        public UserTimelineViewModel Statuses
        {
            get { return _statuses; }
        }

        public UserTimelineViewModel Favorites
        {
            get { return _favorites; }
        }

        public UserFollowingViewModel Following
        {
            get { return _following; }
        }

        public UserFollowersViewModel Followers
        {
            get { return _followers; }
        }

        public bool IsVisibleStatuses
        {
            get { return DisplayKind == UserDisplayKind.Statuses; }
        }

        public bool IsVisibleFavorites
        {
            get { return DisplayKind == UserDisplayKind.Favorites; }
        }

        public bool IsVisibleFollowing
        {
            get { return DisplayKind == UserDisplayKind.Following; }
        }

        public bool IsVisibleFollowers
        {
            get { return DisplayKind == UserDisplayKind.Followers; }
        }

        public bool DisplaySlimView
        {
            get { return _parent.DisplaySlimView; }
        }

        public UserInfoViewModel(SearchFlipViewModel parent, string screenName)
        {
            _parent = parent;
            _screenName = screenName;
            CompositeDisposable.Add(
                parent.ListenPropertyChanged(() => parent.DisplaySlimView,
                    _ => RaisePropertyChanged(() => DisplaySlimView)));
            LoadUser(screenName);
        }

        private async void LoadUser(string screenName)
        {
            try
            {
                var user = await StoreHelper.GetUserAsync(screenName);
                // overwrite by oficially-provided screen name
                _screenName = user.ScreenName;
                RaisePropertyChanged(() => ScreenName);

                User = new UserViewModel(user);
                CompositeDisposable.Add(User);

                Setting.Accounts.Collection
                       .Where(a => a.Id != user.Id)
                       .Select(a => new RelationControlViewModel(this, a, user))
                       .ForEach(RelationControls.Add);

                CompositeDisposable.Add(_statuses = new UserTimelineViewModel(this,
                    new UserTimelineModel(user.Id, TimelineType.User)));
                RaisePropertyChanged(() => Statuses);

                CompositeDisposable.Add(_favorites = new UserTimelineViewModel(this,
                    new UserTimelineModel(user.Id, TimelineType.Favorites)));
                RaisePropertyChanged(() => Favorites);

                CompositeDisposable.Add(_following = new UserFollowingViewModel(this));
                RaisePropertyChanged(() => Following);

                CompositeDisposable.Add(_followers = new UserFollowersViewModel(this));
                RaisePropertyChanged(() => Followers);
            }
            catch (Exception ex)
            {
                _parent.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
                {
                    Title = SearchFlipResources.MsgUserInfoLoadErrorTitle,
                    MainIcon = VistaTaskDialogIcon.Error,
                    MainInstruction =
                        SearchFlipResources.MsgUserInfoLoadErrorInstFormat.SafeFormat(
                            SearchFlipResources.MsgUserProfile),
                    Content = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Close
                }));
                User = null;
                _parent.CloseResults();
            }
            finally
            {
                Communicating = false;
            }
        }

        public void Close()
        {
            Parent.RewindStack();
        }

        public void ShowStatuses()
        {
            DisplayKind = UserDisplayKind.Statuses;
        }

        public void ShowFavorites()
        {
            DisplayKind = UserDisplayKind.Favorites;
        }

        public void ShowFollowing()
        {
            DisplayKind = UserDisplayKind.Following;
        }

        public void ShowFollowers()
        {
            DisplayKind = UserDisplayKind.Followers;
        }

        #region Text selection control

        private string _selectedText;

        public string SelectedText
        {
            get { return _selectedText ?? String.Empty; }
            set
            {
                _selectedText = value;
                RaisePropertyChanged();
            }
        }

        [UsedImplicitly]
        public void CopyText()
        {
            try
            {
                Clipboard.SetText(SelectedText);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
                // ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        [UsedImplicitly]
        public void SetTextToInputBox()
        {
            InputModel.InputCore.SetText(InputSetting.Create(SelectedText));
        }

        [UsedImplicitly]
        public void FindOnKrile()
        {
            SearchFlipModel.RequestSearch(SelectedText, SearchMode.Local);
        }

        [UsedImplicitly]
        public void FindOnTwitter()
        {
            SearchFlipModel.RequestSearch(SelectedText, SearchMode.Web);
        }

        private const string GoogleUrl = @"http://www.google.com/search?q={0}";

        [UsedImplicitly]
        public void FindOnGoogle()
        {
            var encoded = HttpUtility.UrlEncode(SelectedText);
            var url = String.Format(GoogleUrl, encoded);
            BrowserHelper.Open(url);
        }

        #endregion Text selection control

        #region OpenLinkCommand

        private ListenerCommand<string> _openLinkCommand;

        public ListenerCommand<string> OpenLinkCommand
        {
            get { return _openLinkCommand ?? (_openLinkCommand = new ListenerCommand<string>(OpenLink)); }
        }

        public void OpenLink(string parameter)
        {
            var param = TextBlockStylizer.ResolveInternalUrl(parameter);
            switch (param.Item1)
            {
                case LinkType.User:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.UserScreenName);
                    break;
                case LinkType.Hash:
                    SearchFlipModel.RequestSearch("#" + param.Item2, SearchMode.Web);
                    break;
                case LinkType.Url:
                    BrowserHelper.Open(param.Item2);
                    break;
            }
        }

        #endregion OpenLinkCommand
    }

    public enum UserDisplayKind
    {
        Statuses,
        Favorites,
        Following,
        Followers,
    }
}