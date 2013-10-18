using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Subsystems;

namespace StarryEyes.Models.Receiving.Handling
{
    /// <summary>
    /// Broadcast new statuses to each tab, user interfaces, etc
    /// </summary>
    public static class StatusBroadcaster
    {
        private static readonly Subject<StatusNotification> _broadcastSubject = new Subject<StatusNotification>();
        private static readonly ManualResetEvent _signal = new ManualResetEvent(false);
        private static readonly ConcurrentQueue<StatusNotification> _queue = new ConcurrentQueue<StatusNotification>();
        private static Thread _pumpThread;
        private static volatile bool _isHaltRequested;

        public static IObservable<StatusNotification> BroadcastPoint
        {
            get { return _broadcastSubject; }
        }

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
            _pumpThread = new Thread(PumpQueuedStatuses);
            _pumpThread.Start();
        }

        internal static void Queue([NotNull] StatusNotification status)
        {
            if (status == null) throw new ArgumentNullException("status");
            _queue.Enqueue(status);
            _signal.Set();
        }

        public static void Republish([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            _queue.Enqueue(new StatusNotification(status, true, false));
            _signal.Set();
        }

        private static void PumpQueuedStatuses()
        {
            StatusNotification status;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out status) && !_isHaltRequested)
                {
                    if (status.IsAdded && MuteBlockManager.IsBlockedOrMuted(status.Status))
                    {
                        // MUTE CAPTURE
                        System.Diagnostics.Debug.WriteLine("*** Mute or Block Capture: " + status.Status);
                        continue;
                    }
                    if (status.IsAdded && status.IsNew)
                    {
                        NotificationService.NotifyReceived(status.Status);
                        NotificationService.StartAcceptNewArrival(status.Status);
                    }
                    _broadcastSubject.OnNext(status);
                    if (!status.IsAdded)
                    {
                        NotificationService.NotifyDeleted(status.StatusId, status.Status);
                    }
                    else if (status.IsNew)
                    {
                        NotificationService.EndAcceptNewArrival(status.Status);
                    }
                    _signal.Reset();
                }
                if (_isHaltRequested)
                {
                    break;
                }
                _signal.WaitOne();
            }
            _broadcastSubject.OnCompleted();
        }
    }
}
