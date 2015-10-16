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
            AutoUpdateService.UpdateStateChanged += () => this._isUpdateAvailable = true;
            _contributors = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                ContributionService.Contributors, c => new ContributorViewModel(c),
                DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(_contributors.ListenCollectionChanged(
                _ => RaisePropertyChanged(() => IsDonated)));
        }

        public string ApplicationName
        {
            get { return App.AppFullName; }
        }

        [UsedImplicitly]
        public void CheckUpdate()
        {
            Task.Run(() => this.CheckUpdates());
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

        #endregion

        #region Contributors

        private readonly ReadOnlyDispatcherCollectionRx<ContributorViewModel> _contributors;
        public ReadOnlyDispatcherCollectionRx<ContributorViewModel> Contributors
        {
            get { return this._contributors; }
        }

        public bool IsDonated
        {
            get
            {
                var users = ContributionService.Contributors.Select(c => c.ScreenName).ToArray();
                return Setting.Accounts.Collection.Any(c => users.Contains(c.UnreliableScreenName));
            }
        }

        #endregion

        #region Versioning

        public string Version
        {
            get { return App.FormattedVersion; }
        }

        private bool _isChecking;
        private bool _isUpdateAvailable;

        public bool IsChecking
        {
            get { return this._isChecking; }
        }

        public bool IsUpdateIsNotExisted
        {
            get { return !this._isChecking && !this._isUpdateAvailable; }
        }

        public bool IsUpdateExisted
        {
            get { return !this._isChecking && this._isUpdateAvailable; }
        }

        public async void CheckUpdates()
        {
            this._isChecking = true;
            this.RefreshCheckState();
            this._isUpdateAvailable = await AutoUpdateService.CheckPrepareUpdate(App.Version);
            this._isChecking = false;
            this.RefreshCheckState();
        }

        private void RefreshCheckState()
        {
            this.RaisePropertyChanged(() => this.IsChecking);
            this.RaisePropertyChanged(() => this.IsUpdateExisted);
            this.RaisePropertyChanged(() => this.IsUpdateIsNotExisted);
        }

        public void StartUpdate()
        {
            AutoUpdateService.StartUpdate(App.Version);
        }

        #endregion
    }

    public class ContributorViewModel : ViewModel
    {
        private readonly string _name;
        private readonly string _screenName;

        public string Name
        {
            get { return this._name; }
        }

        public string ScreenName
        {
            get { return this._screenName; }
        }

        public bool IsLinkOpenable
        {
            get { return this._screenName != null; }
        }

        public ContributorViewModel(Contributor contributor)
        {
            this._name = contributor.Name;
            this._screenName = contributor.ScreenName;
        }

        public void OpenUserTwitter()
        {
            BrowserHelper.Open(VersionInfoViewModel.Twitter + this._screenName);
        }
    }
}
