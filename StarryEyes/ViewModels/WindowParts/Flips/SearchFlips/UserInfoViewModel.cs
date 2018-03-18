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
        private bool _communicating = true;
        private UserViewModel _user;

        private UserDisplayKind _displayKind = UserDisplayKind.Statuses;

        public UserDisplayKind DisplayKind
        {
            get => _displayKind;
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

        public ObservableCollection<RelationControlViewModel> RelationControls { get; set; } =
            new ObservableCollection<RelationControlViewModel>();

        public SearchFlipViewModel Parent { get; }

        public string ScreenName { get; private set; }

        public bool Communicating
        {
            get => _communicating;
            set
            {
                _communicating = value;
                RaisePropertyChanged();
            }
        }

        public UserViewModel User
        {
            get => _user;
            set
            {
                _user = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsUserAvailable);
            }
        }

        public bool IsUserAvailable => User != null;

        public UserTimelineViewModel Statuses { get; private set; }

        public UserTimelineViewModel Favorites { get; private set; }

        public UserFollowingViewModel Following { get; private set; }

        public UserFollowersViewModel Followers { get; private set; }

        public bool IsVisibleStatuses => DisplayKind == UserDisplayKind.Statuses;

        public bool IsVisibleFavorites => DisplayKind == UserDisplayKind.Favorites;

        public bool IsVisibleFollowing => DisplayKind == UserDisplayKind.Following;

        public bool IsVisibleFollowers => DisplayKind == UserDisplayKind.Followers;

        public bool DisplaySlimView => Parent.DisplaySlimView;

        public UserInfoViewModel(SearchFlipViewModel parent, string screenName)
        {
            Parent = parent;
            ScreenName = screenName;
            CompositeDisposable.Add(
                parent.ListenPropertyChanged(() => parent.DisplaySlimView,
                    _ => RaisePropertyChanged(() => DisplaySlimView)));
            LoadUser(screenName);
        }

        private async void LoadUser(string screenName)
        {
            try
            {
                var user = await StoreHelper.GetUserAsync(screenName, true);
                // overwrite by oficially-provided screen name
                ScreenName = user.ScreenName;
                RaisePropertyChanged(() => ScreenName);

                User = new UserViewModel(user);
                CompositeDisposable.Add(User);

                Setting.Accounts.Collection
                       .Where(a => a.Id != user.Id)
                       .Select(a => new RelationControlViewModel(this, a, user))
                       .ForEach(RelationControls.Add);

                CompositeDisposable.Add(Statuses = new UserTimelineViewModel(this,
                    new UserTimelineModel(user.Id, TimelineType.User)));
                RaisePropertyChanged(() => Statuses);

                CompositeDisposable.Add(Favorites = new UserTimelineViewModel(this,
                    new UserTimelineModel(user.Id, TimelineType.Favorites)));
                RaisePropertyChanged(() => Favorites);

                CompositeDisposable.Add(Following = new UserFollowingViewModel(this));
                RaisePropertyChanged(() => Following);

                CompositeDisposable.Add(Followers = new UserFollowersViewModel(this));
                RaisePropertyChanged(() => Followers);
            }
            catch (Exception ex)
            {
                Parent.Messenger.RaiseSafe(() => new TaskDialogMessage(new TaskDialogOptions
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
                Parent.CloseResults();
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
            get => _selectedText ?? String.Empty;
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

        public ListenerCommand<string> OpenLinkCommand => _openLinkCommand ??
                                                          (_openLinkCommand = new ListenerCommand<string>(OpenLink));

        public void OpenLink(string parameter)
        {
            var param = TextBlockStylizer.ResolveInternalUrl(parameter);
            switch (param.Item1)
            {
                case LinkType.User:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.UserScreenName);
                    break;
                case LinkType.Hash:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.Web);
                    break;
                case LinkType.Symbol:
                    SearchFlipModel.RequestSearch(param.Item2, SearchMode.Web);
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