using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Livet;
using Livet.Messaging.IO;
using StarryEyes.Filters;
using StarryEyes.Filters.Parsing;
using StarryEyes.Globalization.WindowParts;
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
                if (!value)
                {
                    Close();
                }
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
            // ensure close before starting configuration
            this.IsConfigurationActive = false;

            var model = args.Item1;
            var callback = args.Item2;
            this._completeCallback = callback;
            this._currentConfigurationTarget = model;
            this.IsConfigurationActive = true;
            this._lastValidFilterQuery = model.FilterQuery;
            this._initialNormalizedQuery =
                _lastValidFilterQuery == null
                    ? String.Empty
                    : _lastValidFilterQuery.ToQuery();
            _foundError = false;
            _currentQueryString = model.GetQueryString();
            RaisePropertyChanged(() => TabName);
            RaisePropertyChanged(() => IsShowUnreadCounts);
            RaisePropertyChanged(() => IsNotifyNewArrivals);
            RaisePropertyChanged(() => NotifySoundSourcePath);
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

        public string NotifySoundSourcePath
        {
            get { return _currentConfigurationTarget != null ? _currentConfigurationTarget.NotifySoundSource : String.Empty; }
            set
            {
                if (_currentConfigurationTarget == null) return;
                _currentConfigurationTarget.NotifySoundSource = value;
                RaisePropertyChanged();
            }
        }

        [UsedImplicitly]
        public void SelectSoundSource()
        {
            var msg = this.Messenger.GetResponseSafe(() =>
                new OpeningFileSelectionMessage
                {
                    Title = GeneralFlipResources.TabConfigOpenSoundTitle,
                    Filter = GeneralFlipResources.TabConfigOpenSoundFilter + "|*.wav",
                    FileName = NotifySoundSourcePath,
                });
            if (msg.Response != null && msg.Response.Length > 0)
            {
                NotifySoundSourcePath = msg.Response[0];
            }
        }

        private string _initialNormalizedQuery;

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

        private FilterQuery _lastValidFilterQuery;
        private string _lastValidFilterQueryString;

        private async Task<bool> CheckCompile(string source)
        {
            try
            {
                var newFilter = await Task.Run(() => QueryCompiler.Compile(source));
                newFilter.GetEvaluator(); // validate types
                _lastValidFilterQuery = newFilter;
                _lastValidFilterQueryString = source;
                FoundError = false;
                return true;
            }
            catch (Exception ex)
            {
                FoundError = true;
                ExceptionMessage = ex.Message;
                return false;
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

        public async void Close()
        {
            if (!IsConfigurationActive) return;
            this.IsConfigurationActive = false;
            if (_currentConfigurationTarget != null)
            {
                if (this._lastValidFilterQuery != null &&
                    this._lastValidFilterQuery.ToQuery() != this._initialNormalizedQuery)
                {
                    _currentConfigurationTarget.RawQueryString = _lastValidFilterQueryString;
                    _currentConfigurationTarget.FilterQuery = _lastValidFilterQuery;
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
