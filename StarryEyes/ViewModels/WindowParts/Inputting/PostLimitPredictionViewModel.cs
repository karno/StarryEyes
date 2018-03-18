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
            CompositeDisposable.Add(
                InputModel.AccountSelector.Accounts.ListenCollectionChanged(
                    _ => RaisePropertyChanged(() => IsPostLimitPredictionEnabled)));
            CompositeDisposable.Add(
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
                              RaisePropertyChanged(() => RemainUpdate);
                              RaisePropertyChanged(() => MaxUpdate);
                              RaisePropertyChanged(() => ControlWidth);
                              RaisePropertyChanged(() => IsWarningPostLimit);
                          }));
        }

        public bool IsPostLimitPredictionEnabled => InputModel.AccountSelector.Accounts.Count == 1;

        public int RemainUpdate { get; set; }

        public int MaxUpdate { get; set; }

        public bool IsWarningPostLimit => RemainUpdate < 5;

        public int MaxControlWidth => 80;

        public double ControlWidth => (double)MaxControlWidth * RemainUpdate / MaxUpdate;
    }
}