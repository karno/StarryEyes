using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Settings;
using StarryEyes.ViewModels.WindowParts.Backstages;
using StarryEyes.Views.Utils;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    ///     バックパネル ViewModel
    /// </summary>
    public class BackstageViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<BackstageAccountViewModel> _accounts;

        private readonly ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> _twitterEvents;
        private readonly object _syncLock = new object();

        private readonly Queue<BackstageEventBase> _waitingEvents =
            new Queue<BackstageEventBase>();

        private bool _showCurrentEvent;
        private BackstageViewItem _viewItem = BackstageViewItem.TwitterEvents;
        private BackstageEventViewModel _currentEvent;

        private bool _isDisposed;

        public ReadOnlyDispatcherCollectionRx<BackstageAccountViewModel> Accounts
        {
            get { return this._accounts; }
        }

        public ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> TwitterEvents
        {
            get { return this._twitterEvents; }
        }

        public BackstageViewItem ViewItem
        {
            get { return _viewItem; }
            set
            {
                if (_viewItem == value) return;
                _viewItem = value;
                this.RaisePropertyChanged();
            }
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
            if (DesignTimeUtil.IsInDesignMode) return;
            _twitterEvents = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                BackstageModel.TwitterEvents,
                tev => new TwitterEventViewModel(tev),
                DispatcherHelper.UIDispatcher,
                DispatcherPriority.Background);
            CompositeDisposable.Add(_twitterEvents);
            _accounts = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                BackstageModel.Accounts,
                a => new BackstageAccountViewModel(this, a),
                DispatcherHelper.UIDispatcher);
            CompositeDisposable.Add(_accounts);
            CompositeDisposable.Add(
                Observable.FromEvent(
                    h => BackstageModel.CloseBackstage += h,
                    h => BackstageModel.CloseBackstage -= h)
                          .Subscribe(_ => this.Close()));
            CompositeDisposable.Add(
                Observable.FromEvent<BackstageEventBase>(
                    h => BackstageModel.EventRegistered += h,
                    h => BackstageModel.EventRegistered -= h)
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

                var tev = ev as TwitterEventBase;
                if (tev != null && tev.IsLocalUserInvolved)
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

    public enum BackstageViewItem
    {
        TwitterEvents,
        SystemEvents
    }
}