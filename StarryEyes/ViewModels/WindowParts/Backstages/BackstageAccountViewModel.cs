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

        public UserStreamsConnectionState ConnectionState => _model.ConnectionState;

        public bool IsFallbacked { get; set; }

        public DateTime FallbackReleaseTime { get; set; }

        public int RemainUpdate { get; set; }

        public int MaxUpdate { get; set; }

        public bool IsWarningPostLimit => RemainUpdate < 5;

        public BackstageAccountViewModel(BackstageViewModel parent, BackstageAccountModel model)
        {
            _parent = parent;
            _model = model;
            CompositeDisposable.Add(
                Observable.FromEvent(
                              h => _model.ConnectionStateChanged += h,
                              h => _model.ConnectionStateChanged -= h)
                          .Subscribe(_ => ConnectionStateChanged()));
            CompositeDisposable.Add(
                Observable.FromEvent(
                              h => _model.TwitterUserChanged += h,
                              h => _model.TwitterUserChanged -= h)
                          .Subscribe(_ => UserChanged()));
            CompositeDisposable.Add(
                Observable.FromEvent(
                              h => _model.FallbackStateUpdated += h,
                              h => _model.FallbackStateUpdated -= h)
                          .Subscribe(_ => FallbackStateUpdated()));
            CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(5))
                          .Subscribe(_ =>
                          {
                              var count = PostLimitPredictionService.GetCurrentWindowCount(model.Account.Id);
                              MaxUpdate = Setting.PostLimitPerWindow.Value;
                              RemainUpdate = MaxUpdate - count;
                              RaisePropertyChanged(() => RemainUpdate);
                              RaisePropertyChanged(() => MaxUpdate);
                              RaisePropertyChanged(() => IsWarningPostLimit);
                          }));
            CompositeDisposable.Add(() =>
            {
                if (_uvmCache == null) return;
                var cache = _uvmCache;
                _uvmCache = null;
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
            old?.Dispose();
        }

        private void UserChanged()
        {
            RaisePropertyChanged(() => User);
        }

        private void ConnectionStateChanged()
        {
            RaisePropertyChanged(() => ConnectionState);
        }

        private void FallbackStateUpdated()
        {
            IsFallbacked = _model.IsFallbacked;
            FallbackReleaseTime = _model.FallbackPredictedReleaseTime;
            RaisePropertyChanged(() => IsFallbacked);
            RaisePropertyChanged(() => FallbackReleaseTime);
        }

        public void ReconnectUserStreams()
        {
            _model.Reconnect();
        }

        public void OpenProfile()
        {
            if (User == null) return;
            _parent.Close();
            SearchFlipModel.RequestSearch(User.ScreenName, SearchMode.UserScreenName);
        }
    }
}