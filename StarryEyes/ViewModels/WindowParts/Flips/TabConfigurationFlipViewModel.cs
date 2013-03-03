using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Tab;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class TabConfigurationFlipViewModel : ViewModel
    {
        private bool _isConfigurationActive;

        public bool IsConfigurationActive
        {
            get { return _isConfigurationActive; }
            set
            {
                if (_isConfigurationActive == value) return;
                _isConfigurationActive = value;
                MainWindowModel.SetShowMainWindowCommands(!value);
                RaisePropertyChanged();
            }
        }

        private TabModel _currentConfigurationTarget;

        public TabConfigurationFlipViewModel()
        {
            this.CompositeDisposable.Add(Observable.FromEvent<TabModel>(
                h => MainWindowModel.OnTabModelConfigureRaised += h,
                h => MainWindowModel.OnTabModelConfigureRaised -= h)
                .Subscribe(OnConfigurationStart));
        }

        private void OnConfigurationStart(TabModel model)
        {
            this._currentConfigurationTarget = model;
            this.IsConfigurationActive = true;
            _filterQuery = model.FilterQuery;
            _initialQuery = _filterQuery.ToQuery();
            _foundError = false;
            _currentQueryString = model.FilterQueryString;
            RaisePropertyChanged(() => TabName);
            RaisePropertyChanged(() => IsShowUnreadCounts);
            RaisePropertyChanged(() => IsNotifyNewArrivals);
            RaisePropertyChanged(() => QueryString);
            RaisePropertyChanged(() => FoundError);
        }

        public string TabName
        {
            get { return _currentConfigurationTarget != null ? _currentConfigurationTarget.Name : String.Empty; }
            set
            {
                _currentConfigurationTarget.Name = value;
                RaisePropertyChanged();
                MainAreaModel.Save();
            }
        }

        public bool IsShowUnreadCounts
        {
            get { return _currentConfigurationTarget != null && _currentConfigurationTarget.IsShowUnreadCounts; }
            set
            {
                _currentConfigurationTarget.IsShowUnreadCounts = value;
                RaisePropertyChanged();
                MainAreaModel.Save();
            }
        }

        public bool IsNotifyNewArrivals
        {
            get { return _currentConfigurationTarget != null && _currentConfigurationTarget.IsNotifyNewArrivals; }
            set
            {
                _currentConfigurationTarget.IsNotifyNewArrivals = value;
                RaisePropertyChanged();
                MainAreaModel.Save();
            }
        }

        private string _initialQuery;

        private FilterQuery _filterQuery;

        private string _currentQueryString;
        public string QueryString
        {
            get { return _currentQueryString; }
            set
            {
                _currentQueryString = value;
                RaisePropertyChanged();
                CheckCompile(value);
            }
        }

        private bool _foundError;
        public bool FoundError
        {
            get { return _foundError; }
            set
            {
                _foundError = value;
                RaisePropertyChanged();
            }
        }

        private async void CheckCompile(string source)
        {
            try
            {
                var newFilter = await Task.Run(() => QueryCompiler.Compile(source));
                newFilter.GetEvaluator(); // validate types
                _filterQuery = newFilter;
                FoundError = false;
            }
            catch (Exception ex)
            {
                FoundError = true;
                ExceptionMessage = ex.Message;
            }
        }

        private string _exceptionMessage;

        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set
            {
                _exceptionMessage = value;
                RaisePropertyChanged();
            }
        }

        public void Close()
        {
            this.IsConfigurationActive = false;
            if (_currentConfigurationTarget != null)
            {
                if (_filterQuery.ToQuery() != _initialQuery && _currentConfigurationTarget.IsActivated)
                {
                    _currentConfigurationTarget.Deactivate();
                    // update query info
                    _currentConfigurationTarget.FilterQuery = _filterQuery;
                    // finalize
                    _currentConfigurationTarget.Activate();
                    _currentConfigurationTarget.ConfigurationUpdated(true);
                }
                else
                {
                    _currentConfigurationTarget.ConfigurationUpdated(false);
                }
                // regenerate timeline
            }
            MainAreaModel.Save();
        }
    }
}
