using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Mystique.Models.Hub;
using StarryEyes.SweetLady.DataModel;
using StarryEyes.Mystique.Settings;
using StarryEyes.Mystique.Models.Operations;
using StarryEyes.SweetLady.Authorize;
using System.Reactive.Linq;
using StarryEyes.Mystique.Models.Store;
using System.Reactive;

namespace StarryEyes.Mystique.ViewModels.WindowParts.Timeline
{
    public class StatusViewModel : ViewModel
    {
        private TwitterStatus _status;
        public TwitterStatus Status
        {
            get { return _status; }
        }

        private long[] _bindingAccounts;
        public IEnumerable<long> BindingAccounts
        {
            get { return _bindingAccounts; }
            set
            {
                _bindingAccounts = value.ToArray();
                // raise property changed
            }
        }

        public StatusViewModel(TwitterStatus status, IEnumerable<long> initialBoundAccounts)
        {
            this._status = status;
            this._bindingAccounts = initialBoundAccounts.ToArray();
        }

        private UserViewModel _user;
        public UserViewModel User
        {
            get { return _user ?? (_user = new UserViewModel((_status.RetweetedOriginal ?? _status).User)); }
        }

        private UserViewModel _retweeter;
        public UserViewModel Retweeter
        {
            get { return _retweeter ?? (_retweeter = new UserViewModel(_status.User)); }
        }

        private UserViewModel _recipient;
        public UserViewModel Recipient
        {
            get { return _recipient ?? (_recipient = new UserViewModel(_status.Recipient)); }
        }

        public bool IsDirectMessage
        {
            get { return _status.StatusType == StatusType.DirectMessage; }
        }

        public bool IsRetweet
        {
            get { return _status.RetweetedOriginal != null; }
        }

        public bool IsFavorited
        {
            get
            {
                lock (_favoritedsLock)
                {
                    return _bindingAccounts.All(l => _favoritedUsersDic.ContainsKey(l));
                }
            }
        }

        public bool IsRetweeted
        {
            get
            {
                lock (_retweetedsLock)
                {
                    return _bindingAccounts.All(l => _retweetedUsersDic.ContainsKey(l));
                }
            }
        }

        public bool CanFavorite
        {
            get
            {
                return !IsDirectMessage;
            }
        }

        public bool CanFavoriteImmediate
        {
            get
            {
                return CanFavorite && (Setting.AllowFavoriteMyself.Value || !IsMyselfStrict);
            }
        }

        public bool CanRetweet
        {
            get
            {
                return !IsDirectMessage;
            }
        }

        public bool CanRetweetImmediate
        {
            get
            {
                return CanRetweet && !IsMyselfStrict;
            }
        }

        public bool IsMyself
        {
            get { return Setting.Accounts.Any(a => a.UserId == _status.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get
            {
                return this._bindingAccounts.Length == 1 && this._bindingAccounts[0] == _status.User.Id;
            }
        }

        private object _favoritedsLock = new object();
        private SortedDictionary<long, UserViewModel> _favoritedUsersDic = new SortedDictionary<long, UserViewModel>();
        private DispatcherCollection<UserViewModel> _favoritedUsers = new DispatcherCollection<UserViewModel>(DispatcherHelper.UIDispatcher);
        private ReadOnlyDispatcherCollection<UserViewModel> __fuwrap;
        public ReadOnlyDispatcherCollection<UserViewModel> FavoritedUsers
        {
            get { return __fuwrap ?? (__fuwrap = new ReadOnlyDispatcherCollection<UserViewModel>(_favoritedUsers)); }
        }

        public void AddFavoritedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddFavoritedUser);
        }

        public void AddFavoritedUser(TwitterUser user)
        {
            UserViewModel _add = null;
            lock (_favoritedsLock)
            {
                if (!_favoritedUsersDic.ContainsKey(user.Id))
                {
                    _add = new UserViewModel(user);
                    _favoritedUsersDic.Add(user.Id, _add);
                    this._status.FavoritedUsers = this._status.FavoritedUsers.Append(user.Id).ToArray();
                }
            }
            if (_add != null)
            {
                DispatcherHelper.BeginInvoke(() => _favoritedUsers.Add(_add));
                StatusStore.Store(this._status);
            }
        }

        public void RemoveFavoritedUser(long id)
        {
            UserViewModel _remove = null;
            lock (_favoritedsLock)
            {
                if (_favoritedUsersDic.TryGetValue(id, out _remove))
                    this._status.FavoritedUsers = this._status.FavoritedUsers.Except(new[] { id }).ToArray();

            }
            if (_remove != null)
            {
                DispatcherHelper.BeginInvoke(() => _favoritedUsers.Remove(_remove));
                StatusStore.Store(this._status);
            }
        }

        private object _retweetedsLock = new object();
        private SortedDictionary<long, UserViewModel> _retweetedUsersDic = new SortedDictionary<long, UserViewModel>();
        private DispatcherCollection<UserViewModel> _retweetedUsers = new DispatcherCollection<UserViewModel>(DispatcherHelper.UIDispatcher);
        private ReadOnlyDispatcherCollection<UserViewModel> __ruwrap;
        public ReadOnlyDispatcherCollection<UserViewModel> RetweetedUsers
        {
            get { return __ruwrap ?? (__ruwrap = new ReadOnlyDispatcherCollection<UserViewModel>(_retweetedUsers)); }
        }

        public void AddRetweetedUser(long userId)
        {
            StoreHub.GetUser(userId).Subscribe(AddRetweetedUser);
        }

        public void AddRetweetedUser(TwitterUser user)
        {
            UserViewModel _add = null;
            lock (_retweetedsLock)
            {
                if (!_retweetedUsersDic.ContainsKey(user.Id))
                {
                    _add = new UserViewModel(user);
                    _retweetedUsersDic.Add(user.Id, _add);
                    this._status.RetweetedUsers = this._status.RetweetedUsers.Append(user.Id).ToArray();
                }
            }
            if (_add != null)
            {
                DispatcherHelper.BeginInvoke(() => _retweetedUsers.Add(_add));
                // update persistent info
                StatusStore.Store(this._status);
            }
        }

        public void RemoveRetweetedUser(long id)
        {
            UserViewModel _remove = null;
            lock (_retweetedsLock)
            {
                if (_retweetedUsersDic.TryGetValue(id, out _remove))
                    this._status.RetweetedUsers = this._status.RetweetedUsers.Except(new[] { id }).ToArray();
            }
            if (_remove != null)
            {
                DispatcherHelper.BeginInvoke(() => _retweetedUsers.Remove(_remove));
                // update persistent info
                StatusStore.Store(this._status);
            }
        }

        #region Execution commands

        #region ToggleFavoriteImmediateCommand
        private Livet.Commands.ViewModelCommand _ToggleFavoriteImmediateCommand;

        public Livet.Commands.ViewModelCommand ToggleFavoriteImmediateCommand
        {
            get
            {
                if (_ToggleFavoriteImmediateCommand == null)
                {
                    _ToggleFavoriteImmediateCommand = new Livet.Commands.ViewModelCommand(ToggleFavoriteImmediate);
                }
                return _ToggleFavoriteImmediateCommand;
            }
        }

        public void ToggleFavoriteImmediate()
        {
            bool addFav = false;
            Action<AuthenticateInfo> expected = null;
            Action<AuthenticateInfo> onFail = null;
            if (IsFavorited)
            {
                // remove favorite
                addFav = false;
                expected = a => RemoveFavoritedUser(a.Id);
                onFail = a => AddFavoritedUser(a.Id);
            }
            else
            {
                addFav = true;
                expected = a => AddFavoritedUser(a.Id);
                onFail = a => RemoveFavoritedUser(a.Id);
            }

            GetImmediateAccounts()
                .ToObservable()
                .Do(expected)
                .SelectMany(a => new FavoriteOperation(a, this._status, addFav)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        onFail(a);
                        return Observable.Empty<TwitterStatus>();
                    }))
                .Subscribe();
        }
        #endregion

        #region ToggleRetweetImmediateCommand
        private Livet.Commands.ViewModelCommand _ToggleRetweetImmediateCommand;

        public Livet.Commands.ViewModelCommand ToggleRetweetImmediateCommand
        {
            get
            {
                if (_ToggleRetweetImmediateCommand == null)
                {
                    _ToggleRetweetImmediateCommand = new Livet.Commands.ViewModelCommand(ToggleRetweetImmediate);
                }
                return _ToggleRetweetImmediateCommand;
            }
        }

        public void ToggleRetweetImmediate()
        {
            bool retweet = false;
            Action<AuthenticateInfo> expected = null;
            Action<AuthenticateInfo> onFail = null;
            if (IsRetweeted)
            {
                // remove favorite
                retweet = false;
                expected = a => RemoveRetweetedUser(a.Id);
                onFail = a => AddRetweetedUser(a.Id);
            }
            else
            {
                retweet = true;
                expected = a => AddRetweetedUser(a.Id);
                onFail = a => RemoveRetweetedUser(a.Id);
            }

            GetImmediateAccounts()
                .ToObservable()
                .Do(expected)
                .Select(a =>
                    new RetweetOperation(a, this._status, retweet)
                    .Run()
                    .Catch((Exception ex) =>
                    {
                        onFail(a);
                        return Observable.Empty<TwitterStatus>();
                    }))
                .Subscribe();
        }
        #endregion

        private IEnumerable<AuthenticateInfo> GetImmediateAccounts()
        {
            return Setting.Accounts
                .Where(a => _bindingAccounts.Contains(a.UserId))
                .Select(a => a.AuthenticateInfo);
        }

        #endregion
    }
}