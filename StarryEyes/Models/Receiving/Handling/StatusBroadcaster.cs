using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using Cadena.Data;
using JetBrains.Annotations;
using StarryEyes.Models.Subsystems;
using StarryEyes.Models.Timelines.Statuses;

namespace StarryEyes.Models.Receiving.Handling
{
    /// <summary>
    /// Broadcast new statuses to each tab, user interfaces, etc
    /// </summary>
    public static class StatusBroadcaster
    {
        private static readonly Subject<StatusModelNotification> _broadcastSubject =
            new Subject<StatusModelNotification>();

        private static readonly ConcurrentQueue<StatusModelNotification> _queue =
            new ConcurrentQueue<StatusModelNotification>();

        private static readonly ManualResetEventSlim _signal = new ManualResetEventSlim(false);

        private static Thread _pumpThread;

        private static volatile bool _isHaltRequested;

        public static IObservable<StatusModelNotification> BroadcastPoint => _broadcastSubject;

        static StatusBroadcaster()
        {
            App.ApplicationFinalize += () =>
            {
                _isHaltRequested = true;
                _signal.Set();
            };
        }

        internal static void Initialize()
        {
            _pumpThread = new Thread(StatusPump);
            _pumpThread.Start();
        }

        internal static async void Enqueue([CanBeNull] StatusNotification status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            _queue.Enqueue(await StatusModelNotification.FromStatusNotification(status, true).ConfigureAwait(false));
            _signal.Set();
        }

        public static async void Republish([CanBeNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            Republish(await StatusModel.Get(status).ConfigureAwait(false));
        }

        public static void Republish([CanBeNull] StatusModel status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            _queue.Enqueue(new StatusModelNotification(status, true, false));
            _signal.Set();
        }

        private static void StatusPump()
        {
            StatusModelNotification notification;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out notification) && !_isHaltRequested)
                {
                    var status = notification.StatusModel?.Status;
                    var added = notification.IsAdded && status != null;
                    if (added && MuteBlockManager.IsUnwanted(status))
                    {
                        // MUTE CAPTURE
                        System.Diagnostics.Debug.WriteLine("*** Mute or Block Capture: " + status);
                        continue;
                    }
                    if (added && notification.IsNew)
                    {
                        NotificationService.NotifyReceived(status);
                        NotificationService.StartAcceptNewArrival(status);
                    }
                    _broadcastSubject.OnNext(notification);
                    if (!added)
                    {
                        NotificationService.NotifyDeleted(notification.StatusId, status);
                    }
                    else if (notification.IsNew)
                    {
                        NotificationService.EndAcceptNewArrival(status);
                    }
                    _signal.Reset();
                }
                if (_isHaltRequested)
                {
                    break;
                }
                _signal.Wait();
            }
            _signal.Dispose();
            _broadcastSubject.OnCompleted();
        }
    }
}