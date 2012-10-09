using System;
using System.Collections.Generic;
using System.Linq;
using Livet;
using StarryEyes.Models.Hub;
using StarryEyes.Moon.DataModel;
using StarryEyes.Settings;
using StarryEyes.Models.Operations;
using StarryEyes.Moon.Authorize;
using System.Reactive.Linq;
using StarryEyes.Models.Store;
using System.Reactive;
using StarryEyes.Models;

namespace StarryEyes.ViewModels.WindowParts.Timeline
{
    public class StatusViewModel : ViewModel
    {
        public StatusProxy StatusProxy { get; private set; }
        public TwitterStatus OriginalStatus { get { return StatusProxy.Status; } }
        public TwitterStatus Status
        {
            get
            {
                if (StatusProxy.Status.RetweetedOriginal != null)
                    return StatusProxy.Status.RetweetedOriginal;
                else
                    return StatusProxy.Status;
            }
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

        private IEnumerable<AuthenticateInfo> GetImmediateAccounts()
        {
            return Setting.Accounts
                .Where(a => _bindingAccounts.Contains(a.UserId))
                .Select(a => a.AuthenticateInfo);
        }

        public void Reply()
        {
            UIHub.SetText("@" + this.User.ScreenName, inReplyTo: this.Status);
        }

        public void DirectMessage()
        {
        }

        #endregion
    }
}