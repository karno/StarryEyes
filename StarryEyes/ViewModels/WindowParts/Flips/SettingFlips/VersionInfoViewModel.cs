using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Livet;
using StarryEyes.Albireo.Helpers;
using StarryEyes.Models.Subsystems;
using StarryEyes.Settings;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts.Flips.SettingFlips
{
    public class VersionInfoViewModel : ViewModel
    {
        public const string Twitter = "http://twitter.com/";

        public VersionInfoViewModel()
        {
            // when update is available, callback this.
            if (DesignTimeUtil.IsInDesignMode) return;
            AutoUpdateService.UpdateStateChanged += () => _isUpdateAvailable = true;
            _contributors = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                ContributionService.Contributors, c => new ContributorViewModel(c),
                DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_contributors.ListenCollectionChanged(
                _ => RaisePropertyChanged(() => IsDonated)));
        }

        public string ApplicationName => App.AppFullName;

        [UsedImplicitly]
        public void CheckUpdate()
        {
            Task.Run(() => CheckUpdates());
        }

        #region Open links

        public void OpenKriletan()
        {
            BrowserHelper.Open(Twitter + "kriletan");
        }

        public void OpenLicense()
        {
            BrowserHelper.Open(App.LicenseUrl);
        }

        public void OpenDonation()
        {
            BrowserHelper.Open(App.DonationUrl);
        }

        #endregion Open links

        #region Contributors

        private readonly ReadOnlyDispatcherCollectionRx<ContributorViewModel> _contributors;
        public ReadOnlyDispatcherCollectionRx<ContributorViewModel> Contributors => _contributors;

        public bool IsDonated
        {
            get
            {
                var users = ContributionService.Contributors.Select(c => c.ScreenName).ToArray();
                return Setting.Accounts.Collection.Any(c => users.Contains(c.UnreliableScreenName));
            }
        }

        #endregion Contributors

        #region Versioning

        public string Version => App.FormattedVersion;

        private bool _isChecking;
        private bool _isUpdateAvailable;

        public bool IsChecking => _isChecking;

        public bool IsUpdateIsNotExisted => !_isChecking && !_isUpdateAvailable;

        public bool IsUpdateExisted => !_isChecking && _isUpdateAvailable;

        public async void CheckUpdates()
        {
            _isChecking = true;
            RefreshCheckState();
            _isUpdateAvailable = await AutoUpdateService.CheckPrepareUpdate(App.Version);
            _isChecking = false;
            RefreshCheckState();
        }

        private void RefreshCheckState()
        {
            RaisePropertyChanged(() => IsChecking);
            RaisePropertyChanged(() => IsUpdateExisted);
            RaisePropertyChanged(() => IsUpdateIsNotExisted);
        }

        public void StartUpdate()
        {
            AutoUpdateService.StartUpdate(App.Version);
        }

        #endregion Versioning
    }

    public class ContributorViewModel : ViewModel
    {
        private readonly string _name;
        private readonly string _screenName;

        public string Name => _name;

        public string ScreenName => _screenName;

        public bool IsLinkOpenable => _screenName != null;

        public ContributorViewModel(Contributor contributor)
        {
            _name = contributor.Name;
            _screenName = contributor.ScreenName;
        }

        public void OpenUserTwitter()
        {
            BrowserHelper.Open(VersionInfoViewModel.Twitter + _screenName);
        }
    }
}