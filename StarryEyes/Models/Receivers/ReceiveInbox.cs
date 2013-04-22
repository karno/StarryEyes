using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using StarryEyes.Breezy.DataModel;
using StarryEyes.Models.Notifications;
using StarryEyes.Models.Stores;

namespace StarryEyes.Models.Receivers
{
    public static class ReceiveInbox
    {
        private static readonly ManualResetEvent _signal = new ManualResetEvent(false);

        private static readonly ConcurrentQueue<TwitterStatus> _queue = new ConcurrentQueue<TwitterStatus>();

        private static Thread _pumpThread;

        public static void Queue(TwitterStatus status)
        {
            _queue.Enqueue(status);
            _signal.Set();
        }

        internal static void Initialize()
        {
            App.OnApplicationExit += () => _pumpThread.Abort();
            _pumpThread = new Thread(PumpQueuedStatuses);
            _pumpThread.Start();
        }

        private static async void PumpQueuedStatuses()
        {
            TwitterStatus status;
            while (true)
            {
                _signal.Reset();
                while (_queue.TryDequeue(out status))
                {
                    if ((await StatusStore.Get(status.Id).DefaultIfEmpty()) == null)
                    {
                        System.Diagnostics.Debug.WriteLine("*INBOX* accept: " + status);
                        StatusStore.Store(status);
                        NotificationModel.NotifyNewArrival(status);
                    }
                }
                System.Diagnostics.Debug.WriteLine("*INBOX* wait for receiving...");
                _signal.WaitOne();
            }
        }
    }
}
