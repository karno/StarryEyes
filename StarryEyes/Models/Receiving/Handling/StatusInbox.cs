using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StarryEyes.Annotations;
using StarryEyes.Anomaly.TwitterApi.DataModels;
using StarryEyes.Models.Databases;
using StarryEyes.Models.Timelines.Statuses;
using StarryEyes.Settings;

namespace StarryEyes.Models.Receiving.Handling
{
    /// <summary>
    /// Accept received statuses from any sources
    /// </summary>
    public static class StatusInbox
    {
        private static readonly ManualResetEventSlim _signal = new ManualResetEventSlim(false);

        private static readonly ConcurrentQueue<StatusNotification> _queue =
            new ConcurrentQueue<StatusNotification>();

        private static readonly ConcurrentDictionary<long, DateTime> _removes =
            new ConcurrentDictionary<long, DateTime>();

        private static readonly TimeSpan _threshold = TimeSpan.FromMinutes(5);

        private static Thread _pumpThread;

        private static volatile bool _isHaltRequested;

        private static long _lastReceivedTimestamp;

        private static DateTime _cleanupPeriod;

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
            _cleanupPeriod = DateTime.Now;
            _pumpThread = new Thread(StatusPump);
            _pumpThread.Start();
        }

        public static void Enqueue([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            // store original status first
            if (status.RetweetedOriginal != null)
            {
                Enqueue(status.RetweetedOriginal);
            }
            _queue.Enqueue(new StatusNotification(status));
            _signal.Set();
        }

        public static void EnqueueRemoval(long id)
        {
            _queue.Enqueue(new StatusNotification(id));
        }

        private static async void StatusPump()
        {
            StatusNotification n;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out n) && !_isHaltRequested)
                {
                    var status = n.Status;
                    if (n.IsAdded && status != null)
                    {
                        if (Setting.UseLightweightMute.Value && MuteBlockManager.IsUnwanted(status))
                        {
                            // muted
                            continue;
                        }
                        // check registered as removed or not
                        var removed = IsRegisteredAsRemoved(status.Id) ||
                                      (status.RetweetedOriginalId != null &&
                                      IsRegisteredAsRemoved(status.RetweetedOriginalId.Value));
                        // check status is registered as removed or already received
                        if (removed || !await StatusReceived(status))
                        {
                            continue;
                        }
                        StatusBroadcaster.Enqueue(n);
                    }
                    else
                    {
                        StatusDeleted(n.StatusId);
                    }
                    // post next 
                    _signal.Reset();
                }
                if (_isHaltRequested)
                {
                    break;
                }
                _signal.Wait();
            }
        }

        private static async Task<bool> StatusReceived([NotNull] TwitterStatus status)
        {
            if (status == null) throw new ArgumentNullException("status");
            try
            {
                if (await CheckAlreadyExisted(status.Id))
                {
                    // already received
                    return false;
                }
                StatusProxy.StoreStatus(status);
                return true;
            }
            catch (SQLiteException sqex)
            {
                System.Diagnostics.Debug.WriteLine("Requeue: " + status);

                // enqueue for retry 
                Enqueue(status);

                // and return "already received" sign
                return false;
            }
        }

        /// <summary>
        /// Check received status is already existed or not.
        /// </summary>
        /// <param name="id">status id</param>
        /// <returns>if id is already existed, return true.</returns>
        private static async Task<bool> CheckAlreadyExisted(long id)
        {
            // check new status based on timestamps
            var stamp = GetTimestampFromSnowflakeId(id);
            if (_lastReceivedTimestamp == 0)
            {
                _lastReceivedTimestamp = stamp;
            }
            else if (stamp > _lastReceivedTimestamp)
            {
                _lastReceivedTimestamp = stamp;
                return false; // new status
            }
            // check status based on model cache
            if (StatusModel.GetIfCacheIsAlive(id) != null)
            {
                return true; // already existed
            }
            // check with database
            return await StatusProxy.IsStatusExistsAsync(id);
        }

        private static long GetTimestampFromSnowflakeId(long id)
        {
            // [42bit:timestamp][10bit:machine_id][12bit:sequence_id];64bit
            return id >> 22;
        }

        private static bool IsRegisteredAsRemoved(long id)
        {
            return _removes.ContainsKey(id);
        }

        private static void StatusDeleted(long statusId)
        {
            // registered as removed status
            _removes[statusId] = DateTime.Now;
            Task.Run(async () =>
            {
                // find removed statuses
                var removeds = await StatusProxy.RemoveStatusAsync(statusId);

                // notify removed ids
                foreach (var removed in removeds)
                {
                    _removes[removed] = DateTime.Now;
                    StatusBroadcaster.Enqueue(new StatusNotification(removed));
                }

                // check cleanup cycle
                var stamp = DateTime.Now;
                if (stamp - _cleanupPeriod > _threshold)
                {
                    // update period stamp
                    _cleanupPeriod = stamp;

                    // remove expireds
                    _removes.Where(t => (stamp - t.Value) > _threshold)
                            .ForEach(t =>
                            {
                                // remove expired
                                DateTime value;
                                _removes.TryRemove(t.Key, out value);
                            });
                }
            });
        }
    }
}
