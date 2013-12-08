using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Receiving.Handling
{
    /// <summary>
    /// Accept received statuses from any sources
    /// </summary>
    public static class StatusInbox
    {
        private static readonly ManualResetEventSlim _signal = new ManualResetEventSlim(false);

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
                        if (await StatusProxy.IsStatusExistsAsync(n.Status.Id)) continue;
                        // store status
                        await StatusProxy.StoreStatusAsync(n.Status);
                    }
                    else
                    {
                        var removal = await StatusProxy.GetStatusAsync(n.StatusId);
                        var rtt = StatusProxy.GetRetweetedStatusIds(n.StatusId);
                        await StatusProxy.RemoveStatusAsync(n.StatusId);
                        (await rtt).ForEach(QueueRemoval);
                        if (removal != null)
                        {
                            n = new StatusNotification(removal, false);
                        }
                    }
                    // post next 
                    StatusBroadcaster.Queue(n);
                    _signal.Reset();
                }
                if (_isHaltRequested)
                {
                    break;
                }
                _signal.Wait();
            }
        }
    }
}
