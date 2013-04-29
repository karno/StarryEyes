using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Backstages;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Backstages;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    ///     バックパネル ViewModel
    /// </summary>
    public class BackstageViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<BackstageAccountViewModel> _accounts;

        private readonly ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> _events;
        private readonly object _syncLock = new object();

        private readonly Queue<BackstageEventBase> _waitingEvents =
            new Queue<BackstageEventBase>();

        private bool _showCurrentEvent;
        private BackstageEventViewModel _currentEvent;

        private bool _isDisposed;

        public ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> Events
        {
            get { return _events; }
        }

        public ReadOnlyDispatcherCollectionRx<BackstageAccountViewModel> Accounts
        {
            get { return this._accounts; }
        }

        public string CurrentVersion
        {
            get { return App.FormattedVersion; }
        }

        public bool ShowCurrentEvent
        {
            get { return _showCurrentEvent; }
            set
            {
                _showCurrentEvent = value;
                RaisePropertyChanged();
            }
        }

        public bool IsCurrentEventAvailable
        {
            get { return _currentEvent != null; }
        }

        public BackstageEventViewModel CurrentEvent
        {
            get { return _currentEvent; }
            set
            {
                _currentEvent = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => IsCurrentEventAvailable);
            }
        }

        /// <summary>
        ///     for design-time support.
        /// </summary>
        public BackstageViewModel()
        {
            _events = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                BackstageModel.TwitterEvents,
                tev => new TwitterEventViewModel(tev),
                DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_events);
            _accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                BackstageModel.Accounts,
                a => new BackstageAccountViewModel(this, a),
                DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_accounts);
            CompositeDisposable.Add(
                Observable.FromEvent<BackstageEventBase>(
                    h => BackstageModel.OnEventRegistered += h,
                    h => BackstageModel.OnEventRegistered -= h)
                          .Subscribe(ev =>
                          {
                              lock (_syncLock)
                              {
                                  _waitingEvents.Enqueue(ev);
                                  Monitor.Pulse(_syncLock);
                              }
                          }));
            CompositeDisposable.Add(() =>
            {
                lock (_syncLock)
                {
                    _isDisposed = true;
                    Monitor.Pulse(_syncLock);
                }
            });
        }

        public void Initialize()
        {
            Task.Factory.StartNew(EventDispatchWorker,
                                  TaskCreationOptions.LongRunning);
        }

        private void EventDispatchWorker()
        {
            while (true)
            {
                BackstageEventBase ev;
                lock (_syncLock)
                {
                    if (_isDisposed) return;
                    if (_waitingEvents.Count == 0)
                        Monitor.Wait(_syncLock);
                    if (_isDisposed) return;
                    ev = _waitingEvents.Dequeue();
                }
                CurrentEvent = new BackstageEventViewModel(ev);
                ShowCurrentEvent = true;
                Thread.Sleep(Math.Max(Setting.EventDisplayMinimumMSec.Value, 100));
                lock (_syncLock)
                {
                    if (_waitingEvents.Count == 0)
                        Monitor.Wait(_syncLock,
                                     Setting.EventDisplayMaximumMSec.Value - Setting.EventDisplayMinimumMSec.Value);
                    ShowCurrentEvent = false;
                    Thread.Sleep(100);
                }
            }
        }

        public void Close()
        {
            MainWindowModel.TransitionBackstage(false);
        }
    }
}