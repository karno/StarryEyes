using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using StarryEyes.Annotations;
using StarryEyes.Models.Notifications;

namespace StarryEyes.Models.Statuses
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

        public static Subject<StatusNotification> BroadcastPoint
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

        public static void Queue([NotNull] StatusNotification status)
        {
            if (status == null) throw new ArgumentNullException("status");
            _queue.Enqueue(status);
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
                    if (status.IsAdded)
                    {
                        // check global block/mute
                        if (MuteBlockManager.IsBlockedOrMuted(status.Status))
                        {
                            System.Diagnostics.Debug.WriteLine("MUTE OR BLOCK CAPTURE: " + status);
                            continue;
                        }
                    }
                    _broadcastSubject.OnNext(status);
                    if (status.IsAdded)
                    {
                        NotificationModel.NotifyNewArrival(status.Status);
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
