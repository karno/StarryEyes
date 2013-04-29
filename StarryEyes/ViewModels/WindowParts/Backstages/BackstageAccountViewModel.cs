using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Receivers.ReceiveElements;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Timelines;

namespace StarryEyes.ViewModels.WindowParts.Backstages
{
    public class BackstageAccountViewModel : ViewModel
    {
        private readonly BackstageViewModel _parent;
        private readonly BackstageAccountModel _model;
        public UserViewModel User
        {
            get { return _model.User == null ? null : new UserViewModel(_model.User); }
        }

        public UserStreamsConnectionState ConnectionState
        {
            get { return _model.ConnectionState; }
        }

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
            this.CompositeDisposable.Add(Observable.FromEvent(
                h => _model.ConnectionStateChanged += h,
                h => _model.ConnectionStateChanged -= h)
                .Subscribe(_ => ConnectionStateChanged()));
            this.CompositeDisposable.Add(Observable.FromEvent(
                h => _model.TwitterUserChanged += h,
                h => _model.TwitterUserChanged -= h)
                .Subscribe(_ => UserChanged()));
            this.CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(5))
                          .Subscribe(_ =>
                          {
                              var count = PostLimitPredictionService.GetCurrentWindowCount(model.Info.Id);
                              MaxUpdate = Setting.PostLimitPerWindow.Value;
                              RemainUpdate = MaxUpdate - count;
                              this.RaisePropertyChanged(() => RemainUpdate);
                              this.RaisePropertyChanged(() => MaxUpdate);
                              this.RaisePropertyChanged(() => IsWarningPostLimit);
                          }));
        }

        private void UserChanged()
        {
            this.RaisePropertyChanged(() => User);
        }

        private void ConnectionStateChanged()
        {
            this.RaisePropertyChanged(() => ConnectionState);
        }

        public void ReconnectUserStreams()
        {
            this._model.Reconnect();
        }

        public void OpenProfile()
        {
            _parent.Close();
            SearchFlipModel.RequestSearch(this.User.ScreenName, SearchMode.UserScreenName);
        }
    }
}
