using System;
using System.Reactive.Linq;
using System.Threading;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Receiving;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;
using StarryEyes.ViewModels.Timelines.Statuses;

namespace StarryEyes.ViewModels.WindowParts.Backstages
{
    public class BackstageAccountViewModel : ViewModel
    {
        private readonly BackstageViewModel _parent;
        private readonly BackstageAccountModel _model;

        private UserViewModel _uvmCache;

        public UserViewModel User
        {
            get
            {
                UpdateUserCache();
                return _uvmCache;
            }
        }

        public UserStreamsConnectionState ConnectionState
        {
            get { return _model.ConnectionState; }
        }

        public bool IsFallbacked { get; set; }

        public DateTime FallbackReleaseTime { get; set; }

        public int RemainUpdate { get; set; }

        public int MaxUpdate { get; set; }

        public bool IsWarningPostLimit
        {
            get { return RemainUpdate < 5; }
        }

        public BackstageAccountViewModel(BackstageViewModel parent, BackstageAccountModel model)
        {
            _parent = parent;
            _model = model;
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                    h => _model.ConnectionStateChanged += h,
                    h => _model.ConnectionStateChanged -= h)
                          .Subscribe(_ => ConnectionStateChanged()));
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                    h => _model.TwitterUserChanged += h,
                    h => _model.TwitterUserChanged -= h)
                          .Subscribe(_ => UserChanged()));
            this.CompositeDisposable.Add(
                Observable.FromEvent(
                    h => _model.FallbackStateUpdated += h,
                    h => _model.FallbackStateUpdated -= h)
                          .Subscribe(_ => this.FallbackStateUpdated()));
            this.CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(5))
                          .Subscribe(_ =>
                          {
                              var count = PostLimitPredictionService.GetCurrentWindowCount(model.Account.Id);
                              MaxUpdate = Setting.PostLimitPerWindow.Value;
                              RemainUpdate = MaxUpdate - count;
                              this.RaisePropertyChanged(() => RemainUpdate);
                              this.RaisePropertyChanged(() => MaxUpdate);
                              this.RaisePropertyChanged(() => IsWarningPostLimit);
                          }));
            this.CompositeDisposable.Add(() =>
            {
                if (this._uvmCache == null) return;
                var cache = this._uvmCache;
                this._uvmCache = null;
                cache.Dispose();
            });
            UpdateUserCache();
        }

        private void UpdateUserCache()
        {
            var old = Interlocked.Exchange(ref _uvmCache,
                _model.User != null
                    ? new UserViewModel(_model.User)
                    : null);
            if (old != null)
            {
                old.Dispose();
            }
        }

        private void UserChanged()
        {
            this.RaisePropertyChanged(() => User);
        }

        private void ConnectionStateChanged()
        {
            this.RaisePropertyChanged(() => ConnectionState);
        }

        private void FallbackStateUpdated()
        {
            this.IsFallbacked = _model.IsFallbacked;
            this.FallbackReleaseTime = _model.FallbackPredictedReleaseTime;
            this.RaisePropertyChanged(() => IsFallbacked);
            this.RaisePropertyChanged(() => FallbackReleaseTime);
        }

        public void ReconnectUserStreams()
        {
            this._model.Reconnect();
        }

        public void OpenProfile()
        {
            if (this.User == null) return;
            _parent.Close();
            SearchFlipModel.RequestSearch(this.User.ScreenName, SearchMode.UserScreenName);
        }
    }
}