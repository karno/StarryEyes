using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Livet;
using StarryEyes.Models;
using StarryEyes.Models.Backstages;
using StarryEyes.Models.Backstages.TwitterEvents;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    ///     バックパネル ViewModel
    /// </summary>
    public class BackstageViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> _events;
        private readonly object _syncLock = new object();

        private readonly Queue<BackstageEventBase> _waitingEvents =
            new Queue<BackstageEventBase>();

        private BackstageEventViewModel _currentEvent;

        private bool _isDisposed;

        /// <summary>
        ///     for design-time support.
        /// </summary>
        public BackstageViewModel()
        {
            _events = ViewModelHelperRx.CreateReadOnlyDispatcherCollectionRx(
                BackstageModel.TwitterEvents,
                tev => new TwitterEventViewModel(tev),
                DispatcherHelper.UIDispatcher);
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

        public ReadOnlyDispatcherCollectionRx<TwitterEventViewModel> Events
        {
            get { return _events; }
        }

        private bool _showCurrentEvent;

        public bool ShowCurrentEvent
        {
            get { return _showCurrentEvent; }
            set
            {
                _showCurrentEvent = value;
                RaisePropertyChanged();
            }
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

        public bool IsCurrentEventAvailable
        {
            get { return _currentEvent != null; }
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
    }

    public class BackstageEventViewModel : ViewModel
    {
        private readonly BackstageEventBase _sourceEvent;

        public BackstageEventViewModel(BackstageEventBase ev)
        {
            _sourceEvent = ev;
        }

        public BackstageEventBase SourceEvent
        {
            get { return _sourceEvent; }
        }

        public Color Background
        {
            get { return SourceEvent.Background; }
        }

        public Color Foreground
        {
            get { return SourceEvent.Foreground; }
        }

        public string Title
        {
            get { return SourceEvent.Title; }
        }

        public ImageSource TitleImage
        {
            get { return SourceEvent.TitleImage; }
        }

        public bool IsImageAvailable
        {
            get { return SourceEvent.TitleImage != null; }
        }

        public string Detail
        {
            get { return SourceEvent.Detail.Replace("\r", "").Replace("\n", " "); }
        }
    }

    public class TwitterEventViewModel : BackstageEventViewModel
    {
        public TwitterEventViewModel(TwitterEventBase tev)
            : base(tev)
        {
        }

        public TwitterEventBase TwitterEvent
        {
            get { return SourceEvent as TwitterEventBase; }
        }
    }
}