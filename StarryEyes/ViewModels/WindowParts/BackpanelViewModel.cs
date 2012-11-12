using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using StarryEyes.Models;
using StarryEyes.Models.Backpanels;
using System.Windows.Media;
using StarryEyes.Models.Backpanels.TwitterEvents;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Settings;

namespace StarryEyes.ViewModels.WindowParts
{
    /// <summary>
    /// バックパネル ViewModel
    /// </summary>
    public class BackpanelViewModel : ViewModel
    {
        private readonly ReadOnlyDispatcherCollection<TwitterEventViewModel> _events;
        public ReadOnlyDispatcherCollection<TwitterEventViewModel> Events
        {
            get { return _events; }
        }

        private readonly Queue<BackpanelEventBase> _waitingEvents =
            new Queue<BackpanelEventBase>();

        private bool _isDisposed = false;
        private object _syncLock = new object();

        public BackpanelViewModel()
        {
            _events = ViewModelHelper.CreateReadOnlyDispatcherCollection(
                BackpanelModel.TwitterEvents,
                tev => new TwitterEventViewModel(tev),
                DispatcherHelper.UIDispatcher);
            this.CompositeDisposable.Add(new EventListener<Action<BackpanelEventBase>>(
                _ => BackpanelModel.OnEventRegistered += _,
                _ => BackpanelModel.OnEventRegistered -= _,
                ev =>
                {
                    lock (_syncLock)
                    {
                        _waitingEvents.Enqueue(ev);
                        Monitor.Pulse(_syncLock);
                    }
                }));
            this.CompositeDisposable.Add(() =>
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
            Task.Factory.StartNew(() => EventDispatchWorker(),
                TaskCreationOptions.LongRunning);
        }

        private BackpanelEventViewModel _currentEvent = null;
        public BackpanelEventViewModel CurrentEvent
        {
            get { return _currentEvent; }
            set
            {
                _currentEvent = value;
                RaisePropertyChanged(() => CurrentEvent);
                RaisePropertyChanged(() => IsCurrentEventAvailable);
            }
        }

        public bool IsCurrentEventAvailable
        {
            get { return _currentEvent != null; }
        }

        private void EventDispatchWorker()
        {
            BackpanelEventBase ev = null;
            while (true)
            {
                lock (_syncLock)
                {
                    if (_isDisposed) return;
                    if (_waitingEvents.Count == 0)
                        Monitor.Wait(_syncLock);
                    if (_isDisposed) return;
                    ev = _waitingEvents.Dequeue();
                }
                CurrentEvent = new BackpanelEventViewModel(ev);
                Thread.Sleep(Setting.EventDispatchMinimumMSec.Value);
                lock (_syncLock)
                {
                    if (_waitingEvents.Count == 0)
                        Monitor.Wait(_syncLock,
                            Setting.EventDispatchMaximumMSec.Value - Setting.EventDispatchMinimumMSec.Value);
                    if (_waitingEvents.Count == 0)
                        CurrentEvent = null;
                }
            }
        }
    }

    public class BackpanelEventViewModel : ViewModel
    {
        private readonly BackpanelEventBase _sourceEvent;
        public BackpanelEventBase SourceEvent
        {
            get { return _sourceEvent; }
        } 

        public BackpanelEventViewModel(BackpanelEventBase ev)
        {
            this._sourceEvent = ev;
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

        public string Detail
        {
            get { return SourceEvent.Detail.Replace("\r", "").Replace("\n", " "); }
        }
    }

    public class TwitterEventViewModel : BackpanelEventViewModel
    {
        public TwitterEventViewModel(TwitterEventBase tev)
            : base(tev) { }

        public TwitterEventBase TwitterEvent
        {
            get { return this.SourceEvent as TwitterEventBase; }
        }
    }
}
