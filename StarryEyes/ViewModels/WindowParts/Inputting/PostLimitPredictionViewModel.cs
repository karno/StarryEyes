using System;
using System.Linq;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Inputting;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts.Inputting
{
    public class PostLimitPredictionViewModel : ViewModel
    {
        public PostLimitPredictionViewModel()
        {
            this.CompositeDisposable.Add(
                InputModel.AccountSelector.Accounts.ListenCollectionChanged(
                    _ => this.RaisePropertyChanged(() => IsPostLimitPredictionEnabled)));
            this.CompositeDisposable.Add(
                Observable.Interval(TimeSpan.FromSeconds(5))
                          .Where(_ => IsPostLimitPredictionEnabled)
                          .Subscribe(_ =>
                          {
                              var account =
                                  InputModel.AccountSelector.Accounts.FirstOrDefault();
                              if (account == null) return;
                              var count =
                                  PostLimitPredictionService.GetCurrentWindowCount(account.Id);
                              MaxUpdate = Setting.PostLimitPerWindow.Value;
                              RemainUpdate = MaxUpdate - count;
                              this.RaisePropertyChanged(() => RemainUpdate);
                              this.RaisePropertyChanged(() => MaxUpdate);
                              this.RaisePropertyChanged(() => ControlWidth);
                              this.RaisePropertyChanged(() => IsWarningPostLimit);
                          }));
        }

        public bool IsPostLimitPredictionEnabled
        {
            get { return InputModel.AccountSelector.Accounts.Count == 1; }
        }

        public int RemainUpdate { get; set; }

        public int MaxUpdate { get; set; }

        public bool IsWarningPostLimit
        {
            get { return RemainUpdate < 5; }
        }

        public int MaxControlWidth
        {
            get { return 80; }
        }

        public double ControlWidth
        {
            get { return (double)MaxControlWidth * RemainUpdate / MaxUpdate; }
        }
    }
}
