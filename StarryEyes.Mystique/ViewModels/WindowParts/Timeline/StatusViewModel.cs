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
using StarryEyes.Mystique.Models;

namespace StarryEyes.Mystique.ViewModels.WindowParts.Timeline
{
    public class StatusViewModel : ViewModel
    {
        public StatusProxy StatusProxy { get; private set; }
        public TwitterStatus Status { get { return StatusProxy.Status; } }

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
            this.StatusProxy = StatusProxy.Get(status);
            this._bindingAccounts = initialBoundAccounts.ToArray();
        }

        private UserViewModel _user;
        public UserViewModel User
        {
            get { return _user ?? (_user = new UserViewModel((Status.RetweetedOriginal ?? Status).User)); }
        }

        private UserViewModel _retweeter;
        public UserViewModel Retweeter
        {
            get { return _retweeter ?? (_retweeter = new UserViewModel(Status.User)); }
        }

        private UserViewModel _recipient;
        public UserViewModel Recipient
        {
            get { return _recipient ?? (_recipient = new UserViewModel(Status.Recipient)); }
        }

        public bool IsDirectMessage
        {
            get { return Status.StatusType == StatusType.DirectMessage; }
        }

        public bool IsRetweet
        {
            get { return Status.RetweetedOriginal != null; }
        }

        public bool IsFavorited
        {
            get
            {
                return StatusProxy.IsFavorited(_bindingAccounts);
            }
        }

        public bool IsRetweeted
        {
            get
            {
                return StatusProxy.IsRetweeted(_bindingAccounts);
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
            get { return Setting.Accounts.Any(a => a.UserId == Status.User.Id); }
        }

        public bool IsMyselfStrict
        {
            get
            {
                return this._bindingAccounts.Length == 1 && this._bindingAccounts[0] == Status.User.Id;
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
                expected = a => StatusProxy.RemoveFavoritedUser(a.Id);
                onFail = a => StatusProxy.AddFavoritedUser(a.Id);
            }
            else
            {
                addFav = true;
                expected = a => StatusProxy.AddFavoritedUser(a.Id);
                onFail = a => StatusProxy.RemoveFavoritedUser(a.Id);
            }

            GetImmediateAccounts()
                .ToObservable()
                .Do(expected)
                .SelectMany(a => new FavoriteOperation(a, this.Status, addFav)
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
                expected = a => StatusProxy.RemoveRetweetedUser(a.Id);
                onFail = a => StatusProxy.AddRetweetedUser(a.Id);
            }
            else
            {
                retweet = true;
                expected = a => StatusProxy.AddRetweetedUser(a.Id);
                onFail = a => StatusProxy.RemoveRetweetedUser(a.Id);
            }

            GetImmediateAccounts()
                .ToObservable()
                .Do(expected)
                .Select(a =>
                    new RetweetOperation(a, this.Status, retweet)
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