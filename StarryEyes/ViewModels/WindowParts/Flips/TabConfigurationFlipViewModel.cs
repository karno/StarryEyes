using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Models;
using StarryEyes.Models.Timelines.Tabs;

namespace StarryEyes.ViewModels.WindowParts.Flips
{
    public class TabConfigurationFlipViewModel : ViewModel
    {
        private TabModel _currentConfigurationTarget;
        private ISubject<Unit> _completeCallback;

        private bool _isConfigurationActive;
        public bool IsConfigurationActive
        {
            get { return _isConfigurationActive; }
            set
            {
                if (_isConfigurationActive == value) return;
                _isConfigurationActive = value;
                MainWindowModel.SuppressKeyAssigns = value;
                MainWindowModel.SetShowMainWindowCommands(!value);
                RaisePropertyChanged();
            }
        }

        public TabConfigurationFlipViewModel()
        {
            this.CompositeDisposable.Add(Observable.FromEvent<Tuple<TabModel, ISubject<Unit>>>(
                h => MainWindowModel.TabConfigureRequested += h,
                h => MainWindowModel.TabConfigureRequested -= h)
                .Subscribe(this.StartTabConfigure));

        }

        private void StartTabConfigure(Tuple<TabModel, ISubject<Unit>> args)
        {
            var model = args.Item1;
            var callback = args.Item2;
            this._completeCallback = callback;
            this._currentConfigurationTarget = model;
            this.IsConfigurationActive = true;
            _filterQuery = model.FilterQuery;
            _initialQuery = _filterQuery == null ? String.Empty : _filterQuery.ToQuery();
            _foundError = false;
            _currentQueryString = _initialQuery;
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
                TabManager.Save();
            }
        }

        public bool IsShowUnreadCounts
        {
            get { return _currentConfigurationTarget != null && _currentConfigurationTarget.ShowUnreadCounts; }
            set
            {
                _currentConfigurationTarget.ShowUnreadCounts = value;
                RaisePropertyChanged();
                TabManager.Save();
            }
        }

        public bool IsNotifyNewArrivals
        {
            get { return _currentConfigurationTarget != null && _currentConfigurationTarget.NotifyNewArrivals; }
            set
            {
                _currentConfigurationTarget.NotifyNewArrivals = value;
                RaisePropertyChanged();
                TabManager.Save();
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
                Observable.Timer(TimeSpan.FromMilliseconds(100))
                          .Where(_ => _currentQueryString == value)
                          .Subscribe(_ => CheckCompile(value));
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

        #region OpenQueryReferenceCommand
        private Livet.Commands.ViewModelCommand _openQueryReferenceCommand;

        public Livet.Commands.ViewModelCommand OpenQueryReferenceCommand
        {
            get
            {
                if (this._openQueryReferenceCommand == null)
                {
                    this._openQueryReferenceCommand = new Livet.Commands.ViewModelCommand(OpenQueryReference);
                }
                return this._openQueryReferenceCommand;
            }
        }

        public void OpenQueryReference()
        {
            BrowserHelper.Open(App.QueryReferenceUrl);
        }
        #endregion


        public void Close()
        {
            this.IsConfigurationActive = false;
            if (_currentConfigurationTarget != null)
            {
                if (String.IsNullOrEmpty(_currentConfigurationTarget.Name))
                {
                    _currentConfigurationTarget.Name = "(empty)";
                }
                if (_filterQuery != null && _filterQuery.ToQuery() != _initialQuery)
                {
                    _currentConfigurationTarget.FilterQuery = _filterQuery;
                }
                if (_completeCallback != null)
                {
                    _completeCallback.OnNext(Unit.Default);
                    _completeCallback.OnCompleted();
                    _completeCallback = null;
                }
            }
            TabManager.Save();
        }
    }
}
