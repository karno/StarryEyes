using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Statuses
{
    /// <summary>
    /// Accept received statuses from any sources
    /// </summary>
    public static class StatusInbox
    {
        private static readonly ManualResetEvent _signal = new ManualResetEvent(false);

        private static readonly ConcurrentQueue<StatusNotification> _queue = new ConcurrentQueue<StatusNotification>();

        private static Thread _pumpThread;
        private static volatile bool _isHaltRequested;

        static StatusInbox()
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

        public static void Queue([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            // store original status first
            if (status.RetweetedOriginal != null)
            {
                Queue(status.RetweetedOriginal);
            }
            _queue.Enqueue(new StatusNotification(status));
            _signal.Set();
        }

        public static void QueueRemoval(long id)
        {
            _queue.Enqueue(new StatusNotification(id));
        }

        private static async void PumpQueuedStatuses()
        {
            StatusNotification n;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out n) && !_isHaltRequested)
                {
                    if (n.IsAdded)
                    {
                        // check status duplication
                        if ((await StatusStore.Get(n.Status.Id).DefaultIfEmpty()) != null) continue;
                        // store status
                        StatusStore.Store(n.Status);
                        await DatabaseProxy.StoreStatusAsync(n.Status);
                    }
                    else
                    {
                        StatusStore.Remove(n.StatusId);
                        var rtt = DatabaseProxy.GetRetweetedStatusIds(n.StatusId);
                        await DatabaseProxy.RemoveStatusAsync(n.StatusId);
                        (await rtt).ForEach(QueueRemoval);
                    }
                    // post next 
                    StatusBroadcaster.Queue(n);
                    _signal.Reset();
                }
                if (_isHaltRequested)
                {
                    break;
                }
                _signal.WaitOne();
            }
        }

    }
}
