using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Livet;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class VersionInfoViewModel : ViewModel
    {
        public const string Twitter = "http://twitter.com/";

        public VersionInfoViewModel()
        {
            // when update is available, callback this.
            AutoUpdateService.UpdateStateChanged += () => _isUpdateAvailable = true;
            Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromHours(8))
                      .Subscribe(_ => UpdateContributors());
            Task.Run(() => this.CheckUpdates());
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

        public void OpenDonation()
        {
            BrowserHelper.Open(App.DonationUrl);
        }

        #endregion

        #region Contributors

        private readonly ObservableCollection<ContributorsViewModel> _contributors =
            new ObservableCollection<ContributorsViewModel>();
        public ObservableCollection<ContributorsViewModel> Contributors
        {
            get { return this._contributors; }
        }

        private async void UpdateContributors()
        {
            try
            {
                var vms = await Task.Run(async () =>
                {
                    var hc = new HttpClient();
                    var str = await hc.GetStringAsync(App.ContributorsUrl);
                    using (var sr = new StringReader(str))
                    {
                        var xml = XDocument.Load(sr);
                        return xml.Root
                                  .Descendants("contributor")
                                  .Where(
                                      e =>
                                      e.Attribute("visible") == null ||
                                      e.Attribute("visible").Value.ToLower() != "false")
                                  .Select(ContributorsViewModel.FromXml)
                                  .ToArray();
                    }
                });
                await DispatcherHelper.UIDispatcher.InvokeAsync(
                    () =>
                    {
                        this.Contributors.Clear();
                        this.Contributors.Add(new ContributorsViewModel("thanks to:", null));
                        vms.OrderBy(v => v.ScreenName ?? "~" + v.Name)
                           .ForEach(this.Contributors.Add);
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
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
            _isUpdateAvailable = await AutoUpdateService.CheckPrepareUpdate(App.Version);
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
            get { return _screenName != null; }
        }

        public static ContributorsViewModel FromXml(XElement xElement)
        {
            var twitter = xElement.Attribute("twitter");
            return twitter != null
                       ? new ContributorsViewModel(xElement.Value, twitter.Value)
                       : new ContributorsViewModel(xElement.Value, null);
        }

        public ContributorsViewModel(string name, string screenName)
        {
            this._name = name;
            this._screenName = screenName;
        }

        public void OpenUserTwitter()
        {
            BrowserHelper.Open(VersionInfoViewModel.Twitter + _screenName);
        }
    }
}
