using System;
using System.Reactive.Linq;
using Livet;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class VersionInfoViewModel : ViewModel
    {
        private const string Twitter = "http://twitter.com/";

        public VersionInfoViewModel()
        {
            AutoUpdateService.UpdateStateChanged += this.CheckUpdates;
            Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromHours(8))
                      .Subscribe(_ => UpdateContributors());
        }

        #region Open links

        public void OpenOfficial()
        {
            BrowserHelper.Open(App.OfficialUrl);
        }

        public void OpenKarno()
        {
            BrowserHelper.Open(Twitter + "karno");
        }

        public void OpenKriletan()
        {
            BrowserHelper.Open(Twitter + "kriletan");
        }

        public void OpenLicense()
        {
            BrowserHelper.Open(App.LicenseUrl);
        }

        #endregion

        #region Contributors

        private void UpdateContributors()
        {
	    // TODO
        }

        #endregion

        #region Versioning

        public string Version
        {
            get { return App.FormattedVersion; }
        }

        private bool _isChecking = false;
        private bool _isUpdateAvailable = false;

        public bool IsChecking
        {
            get { return _isChecking; }
        }

        public bool IsUpdateIsNotExisted
        {
            get { return !_isChecking && !_isUpdateAvailable; }
        }

        public bool IsUpdateExisted
        {
            get { return !_isChecking && _isUpdateAvailable; }
        }

        public async void CheckUpdates()
        {
            _isChecking = true;
            this.RefreshCheckState();
            _isUpdateAvailable = await AutoUpdateService.PrepareUpdate(App.Version);
            _isChecking = false;
            this.RefreshCheckState();
        }

        private void RefreshCheckState()
        {
            this.RaisePropertyChanged(() => IsChecking);
            this.RaisePropertyChanged(() => IsUpdateExisted);
            this.RaisePropertyChanged(() => IsUpdateIsNotExisted);
        }

        public void StartUpdate()
        {
            AutoUpdateService.StartUpdate(App.Version);
        }

        #endregion
    }

    public class ContributorsViewModel : ViewModel
    {

    }
}
